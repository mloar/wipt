/*
 *	Copyright (c) 2005 Association for Computing Machinery at the University of Illinois at Urbana-Champaign.
 *  All rights reserved.
 * 
 *	Developed by: 		Special Interest Group for Windows Development
 *						ACM@UIUC
 *						http://www.acm.uiuc.edu/sigwin
 *
 *	Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal with the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 *
 *	* Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimers.
 *	* Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimers in the documentation and/or other materials provided with the distribution.
 *	* Neither the names of SIGWin, ACM@UIUC, nor the names of its contributors may be used to endorse or promote products derived from this Software without specific prior written permission. 
 *
 *	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE CONTRIBUTORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS WITH THE SOFTWARE.
 */
// This	is the main	DLL	file.

#include "stdafx.h"
#include "System.Installer.h"

namespace ACM
{
  namespace Sys
  {
    namespace Windows
    {
      namespace Installer
      {

        //    progress information fields
        int iField[4]; //array of record fields to handle INSTALLOGMODE_PROGRESS data
        int  g_iProgressTotal = 0; // total ticks on progress bar
        int  g_iProgress = 0;      // amount of progress
        BOOL g_bForwardProgress = TRUE; //TRUE if the progress bar control should be incremented in a forward direction
        BOOL g_bScriptInProgress = FALSE;
        BOOL g_bEnableActionData; //TRUE if INSTALLOGMODE_ACTIONDATA messages are sending progress information

        //
        //  FUNCTION: FGetInteger(char*& pch)
        //
        //  PURPOSE:  Converts the string (from current pos. to next whitespace or '\0')
        //            to an integer.
        //
        //  COMMENTS: Assumes correct syntax.  Ptr is updated to new position at whitespace
        //            or null terminator.
        //
        int FGetInteger(wchar_t*& rpch)
        {
          wchar_t* pchPrev = rpch;
          int i = 0;
          while (*rpch && *rpch != ' ')
          {
            i *= 10;
            i += *rpch;
            rpch++;
          }
          *rpch = '\0';
          //int i = atoi(pchPrev);
          return i;
        }

        int isdigit(int ch)
        {
          if(ch < 48 || ch > 57)
            return false;
          return true;
        }

        //
        //	FUNCTION: ParseProgressString(LPSTR	sz)
        //
        //	PURPOSE:  Parses the progress data message sent	to the INSTALLUI_HANDLER callback
        //
        //	COMMENTS: Assumes correct syntax.
        //
        BOOL ParseProgressString(LPTSTR sz)
        {
          TCHAR *pch =	sz;
          if (0 == *pch)
            return FALSE; // no	msg

          while (*pch	!= 0)
          {
            TCHAR chField = *pch++;
            pch++; // for ':'
            pch++; // for sp
            switch (chField)
            {
              case '1': // field 1
                {
                  // progress	message	type
                  if (0 == isdigit(*pch))
                    return FALSE; // blank record
                  iField[0] =	*pch++ - '0';
                  break;
                }
              case '2': // field 2
                {
                  iField[1] =	FGetInteger(pch);
                  if (iField[0] == 2 || iField[0]	== 3)
                    return TRUE; //	done processing
                  break;
                }
              case '3': // field 3
                {
                  iField[2] =	FGetInteger(pch);
                  if (iField[0] == 1)
                    return TRUE; //	done processing
                  break;
                }
              case '4': // field 4
                {
                  iField[3] =	FGetInteger(pch);
                  return TRUE; //	done processing
                }
              default: //	unknown	field
                {
                  return FALSE;
                }
            }
            pch++; // for space	(' ') between fields
          }

          return TRUE;
        }


        Guid ApplicationDatabase::findProductByUpgradeCode(Guid	upgradeCode, int index)
        {
          LPCWSTR	code = 0;
          LPWSTR buffer =	0;
          String*	ret;

          try
          {
            String*	temp = String::Concat("{",upgradeCode.ToString()->ToUpper());
            temp = String::Concat(temp,"}");
            code = static_cast<LPCWSTR>(static_cast<void*>(Marshal::StringToHGlobalAuto(temp)));
          }
          catch(ArgumentException*)
          {
            return Guid::Empty;
          }
          catch (OutOfMemoryException*)
          {
            return Guid::Empty;
          }

          buffer = static_cast<LPWSTR>(static_cast<void*>(Marshal::AllocHGlobal(39 * sizeof(wchar_t))));

          unsigned int thecode;
          if((thecode=MsiEnumRelatedProducts(code, 0,	index, buffer))	!= ERROR_SUCCESS)
            return Guid::Empty;

          ret	= Marshal::PtrToStringUni(buffer);

          Marshal::FreeHGlobal(buffer);

          return ret;
        }

        String* ApplicationDatabase::getInstalledVersion(Guid productCode)
        {
          LPWSTR	code = 0;
          LPWSTR buffer =	0;
          String*	ret;

          try
          {
            String*	temp = String::Concat("{",productCode.ToString()->ToUpper());
            temp = String::Concat(temp,"}");
            code = static_cast<LPWSTR>(static_cast<void*>(Marshal::StringToHGlobalAuto(temp)));
          }
          catch(ArgumentException*)
          {
            return NULL;
          }
          catch (OutOfMemoryException*)
          {
            return NULL;
          }

          buffer = static_cast<LPWSTR>(static_cast<void*>(Marshal::AllocHGlobal(14 * sizeof(wchar_t))));
          DWORD ccb = 14 * sizeof(wchar_t);

          if(MsiGetProductInfo(code, INSTALLPROPERTY_VERSIONSTRING, buffer, &ccb) != ERROR_SUCCESS)
            return NULL;

          ret	= Marshal::PtrToStringUni(buffer);
          Marshal::FreeHGlobal(buffer);
          Marshal::FreeHGlobal(code);

          return ret;
        }

        InstallUILevel ApplicationDatabase::setInternalUI(InstallUILevel newLevel)
        {
          return (InstallUILevel)	MsiSetInternalUI((INSTALLUILEVEL)newLevel, NULL);
        }

        void ApplicationDatabase::setProgressHandler(ProgressHandler* handler)
        {
          ApplicationDatabase::handler=handler;
          MsiSetExternalUI(Callbacks::ExternalUIHandler, INSTALLLOGMODE_PROGRESS|INSTALLLOGMODE_ERROR|INSTALLLOGMODE_FATALEXIT, NULL);
        }

        int ApplicationDatabase::ExecuteHandler(UINT iMessageType, LPCTSTR szMessage)
        {
          if(handler)
          {
            if(iMessageType == INSTALLMESSAGE_PROGRESS)
              if(ParseProgressString(const_cast<LPTSTR>(szMessage)))
              {
                // all fields off by 1 due to c array notation
                switch(iField[0])
                {
                  case 0: // reset progress bar
                    {
                      //field 1 = 0, field 2 = total number of ticks, field 3 = direction, field 4 = in progress

                      /* get total number of ticks in progress bar */
                      g_iProgressTotal = iField[1];

                      /* determine direction */
                      if (iField[2] == 0)
                        g_bForwardProgress = TRUE;
                      else // iField[2] == 1
                        g_bForwardProgress = FALSE;

                      /* get current position of progress bar, depends on direction */
                      // if Forward direction, current position is 0
                      // if Backward direction, current position is Total # ticks
                      g_iProgress = g_bForwardProgress ? 0 : g_iProgressTotal;
                      //SendMessage(/*handle to your progress control*/,PBM_SETRANGE,0,MAKELPARAM(0,g_iProgressTotal));
                      // if g_bScriptInProgress, finish progress bar, else reset (and set up according to direction)
                      //SendMessage(/*handle to your progress control*/,PBM_SETPOS,g_bScriptInProgress ? g_iProgressTotal : g_iProgress, 0);
                      handler->Invoke(g_iProgress/g_iProgressTotal);
                      /* determine new state */
                      // if new state = 1 (script in progress), could send a "Please wait..." msg
                      g_bScriptInProgress = (iField[3] == 1) ? TRUE : FALSE;
                      break;
                    }
                  case 1:
                    {
                      //field 1 = 1, field 2 will contain the number of ticks to increment the bar
                      //ignore if field 3 is zero
                      if(iField[2])
                      {
                        // movement direction determined by g_bForwardProgress set by reset progress msg
                        //SendMessage(/*handle to your progress control*/,PBM_SETSTEP,g_bForwardProgress ? iField[1] : -1*iField[1],0);
                        g_bEnableActionData = TRUE;
                      }
                      else
                      {
                        g_bEnableActionData = FALSE;
                      }
                      break;
                    }
                  case 2:
                    {
                      // only act if progress total has been initialized
                      if (0 == g_iProgressTotal)
                        break;
                      //field 1 = 2,field 2 will contain the number of ticks the bar has moved
                      // movement direction determined by g_bForwardProgress set by reset progress msg
                      handler->Invoke((g_iProgress + (g_bForwardProgress ? iField[1] : -1*iField[1])));
                      //SendMessage(/*handle to your progress control*/,PBM_DELTAPOS,g_bForwardProgress ? iField[1] : -1*iField[1],0);
                      break;
                    }
                  case 3: // fall through (we don't care to handle it -- total tick count adjustment)
                  default:
                    {
                      break;
                    }
                }
              }
            /*if(g_bCancelInstall == TRUE)
              {
              return IDCANCEL;
              }
              else*/

          }
          return IDOK;
        }

        int __stdcall ApplicationDatabase::Callbacks::ExternalUIHandler(LPVOID pvContext, UINT iMessageType, LPCTSTR message)
        {	
          ApplicationDatabase::ExecuteHandler(iMessageType,message);
          return 0;
        }

        unsigned int ApplicationDatabase::installProduct(String* sourcePath, String* commandLine)
        {
          LPWSTR path = 0, line = 0;
          try
          {
            path = static_cast<LPWSTR>(static_cast<void*>(Marshal::StringToHGlobalAuto(sourcePath)));
            line = static_cast<LPWSTR>(static_cast<void*>(Marshal::StringToHGlobalAuto(commandLine)));
          }
          catch(ArgumentException*)
          {
            return ERROR_INVALID_PARAMETER;
          }
          catch (OutOfMemoryException*)
          {
            return ERROR_NOT_ENOUGH_MEMORY;
          }
          setInternalUI(None);
          unsigned int ret = MsiInstallProduct(path, line);
          Marshal::FreeHGlobal(path);
          Marshal::FreeHGlobal(line);
          return ret;
        }

        unsigned int ApplicationDatabase::advertiseProduct(String* sourcePath, String* transforms, LANGID language)
        {
          LPWSTR path = 0, trans = 0;
          try
          {
            path = static_cast<LPWSTR>(static_cast<void*>(Marshal::StringToHGlobalAuto(sourcePath)));
            trans =	static_cast<LPWSTR>(static_cast<void*>(Marshal::StringToHGlobalAuto(transforms)));
          }
          catch(ArgumentException*)
          {
            return ERROR_INVALID_PARAMETER;
          }
          catch (OutOfMemoryException*)
          {
            return ERROR_NOT_ENOUGH_MEMORY;
          }
          unsigned int ret = MsiAdvertiseProduct(path, (LPCWSTR)1, trans, language);
          Marshal::FreeHGlobal(path);
          Marshal::FreeHGlobal(trans);
          return ret;
        }

        unsigned int ApplicationDatabase::removeProduct(Guid productCode)
        {
          LPWSTR code = 0;

          try
          {
            String*	temp = String::Concat(/*" /x {"*/"{",productCode.ToString()->ToUpper());
            temp = String::Concat(temp,/*"} /qn"*/ "}");
            code = static_cast<LPWSTR>(static_cast<void*>(Marshal::StringToHGlobalAuto(temp)));
          }
          catch(ArgumentException*)
          {
            return ERROR_INVALID_PARAMETER;
          }
          catch (OutOfMemoryException*)
          {
            return ERROR_NOT_ENOUGH_MEMORY;
          }

          setInternalUI(None);
          UINT ret = MsiConfigureProduct(code, 0, INSTALLSTATE_ABSENT);

          return ret;
        }

        InstallState ApplicationDatabase::getProductState(Guid productCode)
        {
          LPWSTR code = 0;

          try
          {
            String*	temp = String::Concat("{",productCode.ToString()->ToUpper());
            temp = String::Concat(temp,"}");
            code = static_cast<LPWSTR>(static_cast<void*>(Marshal::StringToHGlobalAuto(temp)));
          }
          catch(ArgumentException*)
          {
            return InstallState::InvalidArg;
          }
          catch (OutOfMemoryException*)
          {
            return InstallState::MoreData;
          }

          INSTALLSTATE ret;
          ret = MsiQueryProductState(code);

          Marshal::FreeHGlobal(code);

          return (InstallState)ret;
        }

        String* ApplicationDatabase::getErrorMessage(unsigned int code)
        {
          String* ret;
          TCHAR buffer[1024];

          va_list crap;
          FormatMessage(FORMAT_MESSAGE_FROM_SYSTEM,0,code,0,buffer,1024,&crap);

          ret	= Marshal::PtrToStringUni(buffer);
          return ret;
        }

        Guid ApplicationDatabase::GetPackageUpgradeCode(String* path)
        {
          return Guid::Empty;
        }
      }
    }
  }
}
