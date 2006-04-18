// SystemInstaller.h

#pragma once

using namespace System;
using System::Runtime::InteropServices::Marshal;

namespace ACM
{
  namespace Sys
  {
    namespace Windows
    {
      namespace Installer
      {
        public __delegate void ProgressHandler(double progress);

        public __value enum InstallUILevel
        {
          NoChange = 0,
                   Default = 1,
                   None = 2,
                   Basic = 3,
                   Reduced =4,
                   Full = 5,
                   EndDialog = 0x80,
                   ProgressOnly = 0x40,
                   HideCancel = 0x20,
                   SourceResOnly = 0x100
        };

        public __value enum InstallState
        {
          NotUsed      = -7,  // component disabled
                       BadConfig    = -6,  // configuration data corrupt
                       Incomplete   = -5,  // installation suspended or in progress
                       SourceAbsent = -4,  // run from source, source is unavailable
                       MoreData     = -3,  // return buffer overflow
                       InvalidArg   = -2,  // invalid function argument
                       Unknown      = -1,  // unrecognized product or feature
                       Broken       =  0,  // broken
                       Advertised   =  1,  // advertised feature
                       Removed      =  1,  // component being removed (action state, not settable)
                       Absent       =  2,  // uninstalled (or action state absent but clients remain)
                       Local        =  3,  // installed on local drive
                       Source       =  4,  // run from source, CD or net
                       Default      =  5,  // use default, local or source
        };

        public __gc struct Version
        {
          char major;
          char minor;
          int build;
        };

        public __gc class ApplicationDatabase
        {
          public:
            static Guid findProductByUpgradeCode(Guid upgradeCode, int index);
            static String* getInstalledVersion(Guid productCode);
            static InstallUILevel setInternalUI(InstallUILevel newLevel);
            static void setProgressHandler(ProgressHandler* handler);				
            static unsigned int applyPatch(String* sourcePath);
            static unsigned int installProduct(String* sourcePath, String* commandLine);
            static unsigned int advertiseProduct(String* sourcePath, String* transforms, LANGID language);
            static unsigned int removeProduct(Guid productCode);
            static String* getErrorMessage(unsigned int code);
            static InstallState getProductState(Guid productCode);
            static int ExecuteHandler(UINT iMessageType, LPCTSTR message);
            static Guid GetPackageUpgradeCode(String* path);
          private:
            static ProgressHandler* handler;

            __nogc class Callbacks
            {
              public:
                static int __stdcall ExternalUIHandler(LPVOID pvContext, UINT iMessageType, LPCTSTR message);
            };
        };
      }
    }
  }
}
