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

        if((command == "apply" || command == "install" || command == "remove") && packages == "")
        {
          Console.Error.WriteLine("Error: no packages specified");
          Usage();
          return;
        }

        bool success = true;
        switch(command)
        {
          case "apply":
            Apply(packages.Split(','));
          break;
          case "install":
            foreach(string package in packages.Split(','))
            {
              success = success && Install(package, devel, ignoretransforms, batch);
            }
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

    private static void Apply(string[] patches)
    {
      foreach(string p in patches)
      {
        if(p == "")
          continue;

        try
        {
          object obj = Library.GetProduct(p);
          if(obj == null)
          {
            Console.Error.WriteLine("No such patch " + p);
            continue;
          }
          else if(!(obj is Patch))
          {
            Console.Error.WriteLine(
                p + " is not a patch.  Use the install command to install it."
                );
            continue;
          }

          Patch patch = (Patch)obj;

          Console.Write("Applying patch "+ patch.name + "... ");

          ApplicationDatabase.setProgressHandler(
              new ACM.Sys.Windows.Installer.ProgressHandler(
                ProgressHandler));

          uint ret;
          ret = ApplicationDatabase.applyPatch(patch.URL);
          Console.WriteLine("");

          if(ret != 0)
          {
            Console.Error.WriteLine(
                "Error code {0} returned from applyPatch for "
                + patch.name,ret);
            Console.Error.WriteLine(ApplicationDatabase.getErrorMessage(ret));
          }
        }
        catch(WiptException e)
        {
          Console.WriteLine(e.Message);
        }
      }
      
    }
    
    private static bool Install(string p, bool devel,
        bool ignoretransforms, bool batch)
    {
      try
      {
        object obj = Library.GetProduct(p);
        if(obj == null)
        {
          Console.Error.WriteLine("No such product " + p);
          return false;
        }
        else if(obj is Suite)
        {
          return InstallSuite((Suite)obj, devel, ignoretransforms, batch);
        }
        else if(obj is Patch)
        {
          Console.Error.WriteLine(
              p + " is a patch.  Use the apply command to apply it."
              );
          return false;
        }

        Product product = (Product)obj;

        Version instVersion;

        // TODO: Allow installation of other versions
        instVersion = product.stableVersion;

        if(instVersion == null)
        {
          Console.Error.WriteLine("Specified version not listed for product "
              + product.name);
          return false;
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
          return false;
        }

        InstallState state = ApplicationDatabase.getProductState(productCode);
        if(state != InstallState.Removed && state != InstallState.Absent 
            && state != InstallState.Unknown)
        {
          Console.Error.WriteLine(product.name + " is already the latest version");
          return true;
        }
        
        if(product.dependencies != null)
        {
          foreach(Dependency depend in product.dependencies)
          {
            string c = "";
            string reqproduct = "";
            foreach(Product prod in Library.GetAll())
            {
              if(prod.upgradeCode == depend.upgradeCode)
              {
                reqproduct = prod.name;
                break;                  
              }
            }
            if(reqproduct == "")
            {
              Console.Error.WriteLine(product.name + " is dependent on an unknown product.  Installation aborted.");
              return false;
            }
            if(!batch)
            {
              Console.WriteLine(product.name 
                  + " is dependent on " + reqproduct);
              Console.WriteLine("\r\nWould you like to install it?");
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
              if(!Install(reqproduct, devel, ignoretransforms, batch))
              {
                // Should already have displayed error
                Console.Error.WriteLine("Installation of " + product.name + " aborted.");
                return false;
              }
            }
            else
            {
              return false;
            }
          }
        }

        string transforms = "";
        if(!ignoretransforms && product.transforms != null)
        {
          foreach(Transform transform in product.transforms)
          {
            if(transform.minVersion != null)
            {
              if(transform.minVersion > instVersion)
                continue;
            }
            if(transform.maxVersion != null)
            {
              if(transform.maxVersion < instVersion)
                continue;
            }

            transforms = transform.URL + ";";
          }
        }

        Console.Write("Installing product "+ product.name + "... ");

        ApplicationDatabase.setProgressHandler(
            new ACM.Sys.Windows.Installer.ProgressHandler(
              ProgressHandler));

        uint ret;
        ret = ApplicationDatabase.installProduct(URL,
            (transforms!="REBOOT=R"?"REBOOT=R TRANSFORMS=" + transforms:""));
        Console.WriteLine("");

        if(ret != 0)
        {
          Console.Error.WriteLine(
              "Error code {0} returned from installProduct for "
              + product.name,ret);
          Console.Error.WriteLine(ApplicationDatabase.getErrorMessage(ret));
          return false;

        }

        return true;
      }
      catch(WiptException e)
      {
        Console.Error.WriteLine(e.Message);
        return false;
      }
    }

    private static bool InstallSuite(Suite suite, bool devel,
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
        bool success = true;

        foreach(string product in suite.products)
        {
          success = success && Install(product, devel, ignoretransforms, batch);
        }

        return success;
      }

      return false;
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
          
          if(IsInstalled(product.name))
          {
            Console.Write("Removing product "+ product.name + "... ");

            ApplicationDatabase.setProgressHandler(
                new ACM.Sys.Windows.Installer.ProgressHandler(
                  ProgressHandler));

            Guid productCode = ApplicationDatabase.findProductByUpgradeCode(
                product.upgradeCode, 0);

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
        apply
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
        bool ret = Library.Update();
        if(!ret)
          Console.Error.WriteLine("Could not retrieve all repository files.");
        return ret;
      }
      catch(WiptException e)
      {
        Console.Error.WriteLine(e.Message);
        Console.Error.WriteLine(e.InnerException.Message);
        return false;
      }
      catch(System.Xml.XmlException e)
      {
        Console.Error.WriteLine("There is a problem with a repository file:");
        Console.Error.WriteLine(e.Message);
        return false;
      }
      catch(System.Xml.Schema.XmlSchemaException e)
      {
        Console.Error.WriteLine("The Wipt Schema file is not currently accessible or usable.  Please try again later.");
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
          Console.WriteLine(p.name);
          foreach(Package k in p.packages)
          {
            string installstring="";
            if(p.stableVersion == k.version)
              installstring = "(stable)";
            if(ApplicationDatabase.getProductState(k.productCode)
              == InstallState.Default)
              installstring += "(installed)";
            Console.WriteLine("\tv{0}.{1}.{2} {3}", k.version.major, k.version.minor, k.version.build, installstring);
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
            Console.WriteLine("\n" + p.name);
            foreach(Package k in p.packages)
            {
              string installstring="";
              if(p.stableVersion.Equals(k.version))
                installstring = "(stable)";
              if(ApplicationDatabase.getProductState(k.productCode)
                == InstallState.Default)
                installstring += "(installed)";
              Console.WriteLine("\tv{0}.{1}.{2} {3}", k.version.major, k.version.minor, k.version.build, installstring);
            }
          }
          /*else if(o is Suite)
          {
            Suite u = (Suite)o;
            Console.WriteLine("Suite: " + u.name);
            foreach(string s in u.products)
            {
              Console.WriteLine('\t' + s);
            }
          }
          else if(o is Patch)
          {
            Patch a = (Patch)o;
            Console.WriteLine("Patch: " + a.name);
            foreach(Guid g in a.productCodes)
            {
              Console.WriteLine('\t');
            }
          }*/
        }
      }
      catch(WiptException e)
      {
        Console.WriteLine(e.Message);
      }
    }

    private static bool IsInstalled(string product)
    {
      Object obj = Library.GetProduct(product);

      if(obj == null)
      {
        return false;
      }

      if(obj is Patch)
      {
        return false;
      }
      else if(obj is Suite)
      {
        Suite s = (Suite)obj;
        bool installed = true;
        foreach(string prod in s.products)
        {
          installed = installed && IsInstalled(prod);
        }
        return installed;
      }
      else if(obj is Product)
      {
        Product p = (Product)obj;

        Guid productCode = 
          ApplicationDatabase.findProductByUpgradeCode(
              p.upgradeCode, 0);
        if(productCode == Guid.Empty)        
        {
          return false;
        }

        InstallState state = 
          ApplicationDatabase.getProductState(productCode);
        if(state ==  InstallState.Removed 
            || state == InstallState.Absent 
            || state == InstallState.Unknown)
        {
          return false;
        }

        return true;
      }

      return false;
    }

    private static bool IsInstalled(string product, Version minVersion, Version maxVersion)
    {
      Object obj = Library.GetProduct(product);
      if(obj == null)
      {
        return false;
      }

      if(!(obj is Product))
      {
        return false;
      }

      Product p = (Product)obj;

      Guid productCode = 
        ApplicationDatabase.findProductByUpgradeCode(
            p.upgradeCode,0);
      if(productCode == Guid.Empty)        
      {
        return false;
      }

      InstallState state = 
        ApplicationDatabase.getProductState(productCode);
      if(state == InstallState.Removed 
          || state == InstallState.Absent 
          || state == InstallState.Unknown)
      {
        return false; 
      }

      string[] ver = new string[3];
      ver = ApplicationDatabase.getInstalledVersion(productCode).Split('.');
      
      Version v = new Version(ver[0], ver[1], ver[2]);
      if(v < minVersion || v > maxVersion)
      {
        return false;
      }

      return true;
    }

    private static int index = 0;
    private static void ProgressHandler(double progress)
    {
      if(++index >= 4)
        index = 0;
      char[] bob = new char[] {'\\','|','/','-'};
      Console.Write(new char[] {'\x08', bob[index]});
    }
  }
}
