/*
 *  Copyright (c) 2006 Association for Computing Machinery at the 
 *  University of Illinois at Urbana-Champaign.
 *  All rights reserved.
 * 
 *  Developed by: Special Interest Group for Windows Development
 *                ACM@UIUC
 *                http://www.acm.uiuc.edu/sigwin
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a 
 *  copy of this software and associated documentation files (the "Software"),
 *  to deal with the Software without restriction, including without limitation
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,
 *  and/or sell copies of the Software, and to permit persons to whom the
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  Redistributions of source code must retain the above copyright notice, this
 *  list of conditions and the following disclaimers.
 *  Redistributions in binary form must reproduce the above copyright notice,
 *  this list of conditions and the following disclaimers in the documentation
 *  and/or other materials provided with the distribution.
 *  Neither the names of SIGWin, ACM@UIUC, nor the names of its contributors
 *  may be used to endorse or promote products derived from this Software
 *  without specific prior written permission. 
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  CONTRIBUTORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 *  DEALINGS WITH THE SOFTWARE.
 */

using System;
using System.IO;
using System.Net;
using System.Xml;
using ACM.Wipt.WindowsInstaller;
using Microsoft.Win32;

namespace ACM.Wipt
{
  public class WiptPutter
  {
    [STAThread]
      public static void Main(string[] args)
      {
        string command = "";
        string repofile = "";

        // parameters for addpackage
        string msiurl = "";
        bool makestable = false;

        // parameters for create
        string maintainer = "";
        string supporturl = "";

        foreach(string arg in args)
        {
          if(command == "")
          {
            if(arg.ToLower() == "addpackage" || arg.ToLower() == "create")
            {
              command = arg;
            }
            else
            {
              Console.Error.WriteLine("ERROR: invalid command specified");
              Usage();
              return;
            }
          }
          else
          {
            if(command == "addpackage")
            {
              if(repofile == "")
              {
                if(arg == "--make-stable")
                  makestable = true;
                else
                  repofile = arg;
              }
              else if(msiurl == "")
                msiurl = arg;
              else
              {
                Console.Error.WriteLine("ERROR: too many arguments");
                Usage();
                return;
              }
            }
            else /* if(command == "create") */
            {
              if(repofile == "")
                repofile = arg;
              else if(maintainer == "")
                maintainer = arg;
              else if(supporturl == "")
                supporturl = arg;
              else
              {
                Console.Error.WriteLine("ERROR: too many arguments");
                Usage();
                return;
              }
            }
          }
        }

        Repository theRepo;

        switch(command)
        {
          case "addpackage":
            if(repofile == "" || msiurl == "")
            {
              Console.Error.WriteLine("ERROR: not enough parameters");
              Usage();
              return;
            }
          
            try
            {
              theRepo = new Repository(repofile);
            }
            catch(Exception)
            {
              Console.Error.WriteLine("ERROR: could not open repository file");
              return;
            }

            WebClient wc = new WebClient();
            try
            {
              string tempy = Environment.GetFolderPath(Environment.SpecialFolder.InternetCache) + "\\wiptput.tmp";
              System.IO.File.Delete(tempy);
              wc.DownloadFile(msiurl, tempy);
              MsiDatabase db = new MsiDatabase(tempy);
              theRepo.AddPackage(db.ProductName, db.UpgradeCode, db.Manufacturer, db.SupportURL,
                db.ProductVersion, db.ProductCode, msiurl, makestable);
            }
            catch(Exception)
            {
              Console.Error.WriteLine("ERROR: could not open MSI database");
              return;
            } 
            theRepo.Save(repofile);
          break;
          case "create":
            if(repofile == "" || maintainer == "" || supporturl == "")
            {
              Console.Error.WriteLine("ERROR: not enough parameters");
              Usage();
              return;
            }
            
            try
            {
              Repository.Create(repofile, maintainer, supporturl);
            }
            catch(Exception)
            {
              Console.Error.WriteLine("ERROR: Could not create repository file.");
              return;
            }
          break;
          default:
            Console.Error.WriteLine("ERROR: no command specified");
            Usage();
          break;
        }
      }

    public static void Usage()
    {
      string usage = @"
    Usage: wipt-put create <Repository File> <Maintainer> <Support URL>
           wipt-put addpackage [--make-stable] <Repository File> <Package URL>
";
      Console.WriteLine(usage);
    }
  }
}
