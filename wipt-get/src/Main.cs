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
  public class WiptGetter
  {
    [STAThread]
      public static void Main(string[] args)
      {
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

        bool success = true;
        switch(command.ToLower())
        {
          case "install":
            foreach(string package in packages.Split(','))
            {
              success = success && Install(package, ignoretransforms, batch);
            }
          break;
          case "remove":
            Remove(packages.Split(','));
          break;
          case "show":
            List();
          break;
          case "upgrade":
            Upgrade(ignoretransforms);
          break;
          case "dist-upgrade":
            DistUpgrade(ignoretransforms, batch);
          break;
          case "update":
            Update();
          break;
          default:
            Usage();
          break;
      }
    }

    private static bool Install(string p, bool ignoretransforms, bool batch)
    {
      try
      {
        if(p == "")
          return true;
        Version instVersion = new Version("0","0","0");
        string[] parts = p.Split('=');
        if(parts.Length > 1)
        {
          instVersion = StringToVersion(parts[1]);
        }
        object obj = Library.GetProduct(parts[0]);
        if(obj == null)
        {
          Console.Error.WriteLine("No such product " + parts[0]);
          return false;
        }
        else if(obj is Suite)
        {
          return InstallSuite((Suite)obj, ignoretransforms, batch);
        }

        Product product = (Product)obj;

        if(instVersion == new Version("0","0","0"))
          instVersion = product.stableVersion;

        string URL = "";
        string transforms = "";
        string targetdir = "";
        string otherprops = "";
        Patch[] patches = null;
        string temp;
        if((temp = WiptConfig.GetTargetPath()) != null)
        {
          targetdir = temp;
        }
        if(!ignoretransforms && product.transforms != null)
        {
          foreach(Transform transform in product.transforms)
          {
            if((transform.minVersion == null || instVersion >= transform.minVersion) && (transform.maxVersion == null || instVersion <= transform.maxVersion))
              transforms = transform.URL + ";";
          }
        }
        Guid productCode = Guid.Empty;
        patches = product.patches;
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
          if(instVersion == StringToVersion(ApplicationDatabase.getInstalledVersion(productCode)))
          {
            Console.Error.WriteLine(product.name + " is already the latest version");
            return true;
          }
          else
          {
            otherprops = "REINSTALL=ALL REINSTALLMODE=vomus";
          }
        }

        if(targetdir == "")
        {
          targetdir = "\"" + Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "\"";
          Console.Error.WriteLine("Warning: no TARGETDIR specified.  Defaulting to " + targetdir);
        }

        Console.Write("Installing product "+ product.name + "... ");

        ApplicationDatabase.setProgressHandler(
            new ACM.Sys.Windows.Installer.ProgressHandler(
              ProgressHandler));

        uint ret;
        ret = ApplicationDatabase.installProduct(URL,
            (transforms != "" ? otherprops + " REBOOT=R TARGETDIR=" + targetdir + " TRANSFORMS=" + transforms :
             otherprops + " REBOOT=R TARGETDIR=" + targetdir));
        Console.WriteLine("");

        if(ret != 0)
        {
          Console.Error.WriteLine(
              "Error code {0} returned from installProduct for "
              + product.name,ret);
          Console.Error.WriteLine(ApplicationDatabase.getErrorMessage(ret));
          return false;

        }

        ApplyPatches(patches, productCode);


        return true;
      }
      catch(WiptException e)
      {
        Console.Error.WriteLine(e.Message);
        return false;
      }
    }

    private static bool InstallSuite(Suite suite, bool ignoretransforms, bool batch)
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
          success = success && Install(product, ignoretransforms, batch);
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
        Version instVersion = new Version("0","0","0");
        string[] parts = p.Split('=');
        if(parts.Length > 1)
        {
          instVersion = StringToVersion(parts[1]);
        }
        else
        {
          instVersion = null;
        }
        try
        {
          object obj = Library.GetProduct(parts[0]);
          if(obj == null)
          {
            Console.WriteLine("Could not find product " + parts[0]);
            continue;
          }
          else if(obj is Suite)
          {
            RemoveSuite((Suite)obj);
            continue;
          }

          Product product = (Product)obj;

          if(instVersion == null ? IsInstalled(product.name) : IsInstalled(product.name, instVersion, instVersion))
          {
            Console.Write("Removing product "+ product.name + "... ");

            ApplicationDatabase.setProgressHandler(
                new ACM.Sys.Windows.Installer.ProgressHandler(
                  ProgressHandler));

            Guid productCode = GetVersionProductCode(product.upgradeCode, instVersion);

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
            Console.WriteLine(product.name + " is not installed");
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

    public static void ApplyPatches(Patch[] patches, Guid productCode)
    {
      if(patches != null)
      {
        foreach(Patch patch in patches)
        {
          if(patch.productCodes == null)
          {
            patch.productCodes = new Guid[1];
            patch.productCodes[0] = productCode;
          }
          foreach(Guid code in patch.productCodes)
          {
            if(code == productCode)
            {
              Console.Write("Applying patch "+ patch.name + "... ");
              uint ret = ApplicationDatabase.applyPatch(patch.URL, productCode);
              Console.WriteLine("");
              if(ret != 0)
              {
                Console.Error.WriteLine(
                    "Error code {0} returned from applyPatch for "
                    + patch.name, ret);
                Console.Error.WriteLine(ApplicationDatabase.getErrorMessage(ret));
              }
              break;
            }
          }
        }
      }
    }

    public static void Usage()
    {
      string usage = @"
        Usage:	wipt-get [options] <command> <product>[=version][, <product>[=version], ...]
        OPTIONS
        --batch                     Don't ask any questions
        --ignore-transforms         Don't apply transforms listed in repository

        COMMANDS
        install
        remove
        update
        upgrade
        dist-upgrade
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
      catch(System.Xml.Schema.XmlSchemaException)
      {
        Console.Error.WriteLine("The Wipt Schema file is not currently accessible or usable.  Please try again later.");
        return false;
      }
    }

    public static void Upgrade(bool ignoretransforms)
    {
      object[] list = Library.GetAll();
      foreach(object o in list)
      {
        if(o is Product)
        {
          Guid prod;
          int k = 0;
          Product p = (Product)o;
          
          while((prod = ApplicationDatabase.findProductByUpgradeCode(p.upgradeCode, k)) != Guid.Empty)
          {
            foreach(Package g in p.packages)
            {
              if(g.productCode == prod)
              {
                if(StringToVersion(ApplicationDatabase.getInstalledVersion(prod)) < g.version)
                {
                  Install(p.name + "=" + g.version.ToString(), ignoretransforms, true);
                  break;
                }

                ApplyPatches(p.patches, prod);
              }
            }

            k++;
          }
        }
      }
    }

    public static void DistUpgrade(bool ignoretransforms, bool batch)
    {
      try
      {
        object[] list = Library.GetAll();
        foreach(object o in list)
        {
          if(o is Product)
          {
            Product p = (Product)o;
            if(IsInstalled(p.name) && !IsInstalled(p.name, p.stableVersion, new Version("99.99.9999")))
            {
              Install(p.name, ignoretransforms, batch);
            }
          }
        }
      }
      catch(WiptException e)
      {
        Console.Error.Write(e.Message);
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
              if(p.stableVersion == k.version)
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

    private static Guid GetVersionProductCode(Guid upgradecode, Version version)
    {
      Guid ret;
      int i = 0;
      while((ret = ApplicationDatabase.findProductByUpgradeCode(upgradecode, i)) != Guid.Empty)
      {
        if(version == null || version == StringToVersion(ApplicationDatabase.getInstalledVersion(ret)))
        {
          return ret;
        }
        else
        {
          i++;
        }
      }

      return Guid.Empty;
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

    private static Version StringToVersion(string version)
    {
      string[] ver = version.Split('.');
      if(ver.Length == 1)
      {
        return new Version(ver[0], "", "");
      }
      else if(ver.Length == 2)
      {
        return new Version(ver[0], ver[1], "");
      }
      else
      {
        return new Version(ver[0], ver[1], ver[2]);
      }
    }

    private static int index = 0;
    private static void ProgressHandler(double progress)
    {
      if(++index >= 4)
        index = 0;
      char[] bob = new char[] {'\\','|','/','-'};
      Console.Write(new char[] {'\x08', bob[index]});
    }

    private class WiptConfig
    {
      public static string GetTargetPath()
      {
        string targetdir = null;
        RegistryKey rk = Registry.LocalMachine.OpenSubKey("SOFTWARE\\ACM\\Wipt");
        if(rk != null)
        { 
          object temp = rk.GetValue("TargetDir");
          if(temp == null)
          {
          }
          else if(!(temp is string))
          {
            Console.Error.WriteLine("HKLM\\ACM\\Wipt\\TargetDir is not a REG_SZ - ignored");
          }
          else
          {
            targetdir = (string)temp;
          }
          rk.Close();
        }
        rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\ACM\\Wipt");
        if(rk != null)
        {
          object temp = rk.GetValue("TargetDir");
          if(temp == null)
          {
          }
          else if(!(temp is string))
          {
            Console.Error.WriteLine("HKCU\\ACM\\Wipt\\TargetDir is not a REG_SZ - ignored");
          }
          else
          {
            targetdir = (string)temp;
          }
          rk.Close();
        }

        return targetdir;
      }
    }
  }
}
