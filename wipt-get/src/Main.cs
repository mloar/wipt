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
using ACM.Sys.Windows.Installer;
using Microsoft.Win32;

namespace ACM.Wipt
{
  /// <remarks>
  /// Summary description for Main.
  /// </remarks>
  public class WiptGetter
  {
    [STAThread]
      public static void Main(string[] args)
      {
        bool devel = false;
        bool ignoretransforms = false;
        bool batch = false;
        string command = "";
        string packages = "";

        foreach(string arg in args)
        {
          switch(arg.ToLower())
          {
            case "--batch":
              batch = true;
            break;
            case "--devel":
              devel = true;
            break;
            case "--ignore-transforms":
              ignoretransforms = true;
            break;
            default:
            if(command == "")
              command = arg.ToLower();
            else
              packages += arg + ",";
            break;
          }
        }

        if(command == "")
        {
          Console.Error.WriteLine("Error: no command specified");
          Usage();
          return;
        }

        if((command == "install" || command == "remove") && packages == "")
        {
          Console.Error.WriteLine("Error: no packages specified");
          Usage();
          return;
        }

        switch(command)
        {
          case "install":
            Install(packages.Split(','), devel, ignoretransforms, batch);
          break;
          case "remove":
            Remove(packages.Split(','));
          break;
          case "show":
            List();
          break;
          case "upgrade":
            Upgrade();
          break;
          case "update":
            Update();
          break;
          default:
          Usage();
          break;
        }

      }

    private static void Install(string[] packages, bool devel,
        bool ignoretransforms, bool batch)
    {
      foreach(string p in packages)
      {
        if(p == "")
          continue;

        try
        {
          object obj = Library.GetProduct(p);
          if(obj == null)
          {
            Console.Error.WriteLine("No such product " + p);
            continue;
          }
          else if(obj is Suite)
          {
            InstallSuite((Suite)obj, devel, ignoretransforms, batch);
            continue;
          }

          Product product = (Product)obj;

          Version instVersion;

          if(devel)
            instVersion = product.develVersion;
          else
            instVersion = product.stableVersion;

          if(instVersion == null)
          {
            Console.Error.WriteLine("Specified version not listed for product "
                + product.name);
            continue;
          }

          string URL = "";
          Guid productCode = Guid.Empty;
          if(product.packages != null)
          {
            foreach(Package package in product.packages)
            {
              if(package.version.major == instVersion.major
                  && package.version.minor == instVersion.minor
                  && package.version.build == instVersion.build)
              {
                URL = package.URL;
                productCode = package.productCode;
                break;
              }
            }
          }
          if(URL == "")
          {
            Console.Error.WriteLine("No package listed for specified version of "
                + product.name + ".  Contact the repository maintainer.");
            continue;
          }

          InstallState state = ApplicationDatabase.getProductState(productCode);
          if(state != InstallState.Removed && state != InstallState.Absent 
              && state != InstallState.Unknown)
          {
            Console.Error.WriteLine(product.name + " is already the latest version");
            continue;
          }

          string transforms = "";
          if(!ignoretransforms && product.transforms != null)
          {
            foreach(Transform transform in product.transforms)
            {
              if(transform.version.major == instVersion.major
                  && transform.version.minor == instVersion.minor)
              {
                transforms = transform.URL + ";";
              }
            }
          }

          Console.Write("Installing product "+ product.name + "... ");

          ApplicationDatabase.setProgressHandler(
              new ACM.Sys.Windows.Installer.ProgressHandler(
                ProgressHandler));

          uint ret;
          ret = ApplicationDatabase.installProduct(URL,
              (transforms!=""?"TRANSFORMS=" + transforms:""));
          Console.WriteLine("");

          if(ret != 0)
          {
            Console.Error.WriteLine(
                "Error code {0} returned from installProduct for "
                + product.name,ret);
            Console.Error.WriteLine(ApplicationDatabase.getErrorMessage(ret));
          }
        }
        catch(WiptException e)
        {
          Console.WriteLine(e.Message);
        }
      }
    }

    private static void InstallSuite(Suite suite, bool devel,
        bool ignoretransforms, bool batch)
    {
      string c = "";
      if(!batch)
      {
        Console.WriteLine(suite.name 
            + " is a suite consisting of the following products:");
        foreach(string s in suite.products)
        {
          Console.WriteLine(s);
        }
        Console.WriteLine("\r\nWould you like to install them?");
        Console.Write("(y or n): ");
        c = Console.ReadLine();
        while(c != "y" && c != "Y" && c != "n" && c != "N")
        {
          Console.Write("\r\nPlease type 'y' or 'n': ");
          c = Console.ReadLine();
        }
      }
      if(c == "y" || c == "Y" || batch)
      {
        Install(suite.products, devel, ignoretransforms, batch);
      }
    }

    private static void Remove(string[] packages)
    {
      foreach(string p in packages)
      {
        if(p == "")
          continue;
        try
        {
          object obj = Library.GetProduct(p);
          if(obj == null)
          {
            Console.WriteLine("Could not find product " + p);
            continue;
          }
          else if(obj is Suite)
          {
            RemoveSuite((Suite)obj);
            continue;
          }

          Product product = (Product)obj;
          Guid productCode = 
            ApplicationDatabase.findProductByUpgradeCode(
                product.upgradeCode,0);
          if(productCode != Guid.Empty)
          {
            InstallState state = 
              ApplicationDatabase.getProductState(productCode);
            if(state != InstallState.Removed 
                && state != InstallState.Absent 
                && state != InstallState.Unknown)
            {
              Console.Write("Removing product "+ product.name + "... ");

              ApplicationDatabase.setProgressHandler(
                  new ACM.Sys.Windows.Installer.ProgressHandler(
                    ProgressHandler));

              uint ret = ApplicationDatabase.removeProduct(productCode);
              Console.WriteLine("");
              if(ret != 0)
              {
                Console.WriteLine(
                    "Error code {0} returned from removeProduct",ret);
                Console.WriteLine(ApplicationDatabase.getErrorMessage(ret));
              }
            }
            else
              Console.WriteLine(p + " is not installed");
          }
          else
            Console.WriteLine(p + " is not installed");
        }
        catch(WiptException e)
        {
          Console.WriteLine(e.Message);
        }
      }
    }

    private static void RemoveSuite(Suite suite)
    {
      Console.WriteLine(suite.name 
          + " is a suite consisting of the following products:");
      foreach(string s in suite.products)
      {
        Console.WriteLine(s);
      }
      Console.WriteLine("\r\nWould you like to remove them?");
      Console.Write("(y or n): ");
      string c = Console.ReadLine();
      while(c != "y" && c != "Y" && c != "n" && c != "N")
      {
        Console.Write("\r\nPlease type 'y' or 'n': ");
        c = Console.ReadLine();
      }
      if(c == "y" || c == "Y")
      {
        Remove(suite.products);
      }
    }

    public static void Usage()
    {
      string usage = @"
        Usage:	wipt-get [options] <command> <product>[ <product> <product> ...]
        OPTIONS
        --batch                     Don't ask any questions
        --devel                     Install development version
        --ignore-transforms         Don't apply transforms listed in repository

        COMMANDS
        install
        remove
        update
        upgrade
        show
        ";
      Console.WriteLine(usage);
    }

    public static bool Update()
    {
      try
      {
        return Library.Update();
      }
      catch(WiptException e)
      {
        Console.WriteLine(e.Message);
        return false;
      }
    }

    public static void Upgrade()
    {
      object[] list = Library.GetAll();
      foreach(object o in list)
      {
        if(o is Product)
        {
          Product p = (Product)o;
          if(ApplicationDatabase.findProductByUpgradeCode(p.upgradeCode, 0) != Guid.Empty)
          {
            Console.WriteLine(p.name + "is installed, bitch!");
          }
          Console.WriteLine(p.name);
          if(p.stableVersion != null)
          {
            string installstring="";
            foreach(Package k in p.packages)
            {
              if(p.stableVersion.major == k.version.major
                  && p.stableVersion.minor == k.version.minor
                  && p.stableVersion.build == k.version.build)
              {
                if(ApplicationDatabase.getProductState(k.productCode)
                    == InstallState.Default)
                  installstring="(installed)";
              }
            }
            Console.WriteLine("\tStable Version: {0}.{1}.{2} {3}",
                p.stableVersion.major, p.stableVersion.minor,
                p.stableVersion.build, installstring);
          }
          if(p.develVersion != null)
          {
            string installstring="";
            foreach(Package k in p.packages)
            {
              if(p.develVersion.major == k.version.major
                  && p.develVersion.minor == k.version.minor
                  && p.develVersion.build == k.version.build)
              {
                if(ApplicationDatabase.getProductState(k.productCode)
                    == InstallState.Default)
                  installstring="(installed)";
              }
            }
            Console.WriteLine("\tDevelopment Version: {0}.{1}.{2} {3}",
                p.develVersion.major, p.develVersion.minor,
                p.develVersion.build, installstring);
          }
        }
      }
    }

    public static void List()
    {
      try
      {
        object[] list = Library.GetAll();
        foreach(object o in list)
        {
          if(o is Product)
          {
            Product p = (Product)o;
            Console.WriteLine(p.name);
            if(p.stableVersion != null)
            {
              string installstring="";
              foreach(Package k in p.packages)
              {
                if(p.stableVersion.major == k.version.major
                    && p.stableVersion.minor == k.version.minor
                    && p.stableVersion.build == k.version.build)
                {
                  if(ApplicationDatabase.getProductState(k.productCode)
                      == InstallState.Default)
                    installstring="(installed)";
                }
              }
              Console.WriteLine("\tStable Version: {0}.{1}.{2} {3}",
                  p.stableVersion.major, p.stableVersion.minor,
                  p.stableVersion.build, installstring);
            }
            if(p.develVersion != null)
            {
              string installstring="";
              foreach(Package k in p.packages)
              {
                if(p.develVersion.major == k.version.major
                    && p.develVersion.minor == k.version.minor
                    && p.develVersion.build == k.version.build)
                {
                  if(ApplicationDatabase.getProductState(k.productCode)
                      == InstallState.Default)
                    installstring="(installed)";
                }
              }
              Console.WriteLine("\tDevelopment Version: {0}.{1}.{2} {3}",
                  p.develVersion.major, p.develVersion.minor,
                  p.develVersion.build, installstring);
            }
          }
          else if(o is Suite)
          {
            Suite u = (Suite)o;
            Console.WriteLine(u.name);
            foreach(string s in u.products)
            {
              Console.WriteLine('\t' + s);
            }
          }
        }
      }
      catch(WiptException e)
      {
        Console.WriteLine(e.Message);
      }
    }

    private static int index;
    private static void ProgressHandler(double progress)
    {
      if(++index >= 4)
        index = 0;
      char[] bob = new char[] {'\\','|','/','-'};
      Console.Write(new char[] {'\x08',bob[index]});
    }
  }
}
