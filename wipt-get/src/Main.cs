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
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.Win32;

namespace ACM.Wipt
{
  public class wipt_get
  {
    [DllImport("kernel32.dll")]
      private static extern uint FormatMessage(uint dwFlags, IntPtr lpSource, uint dwMessageId, uint dwLanguageId, 
          StringBuilder lpBuffer, uint nSize, IntPtr args);

    [STAThread]
      public static int Main(string[] args)
      {
        try
        {
          bool ignoretransforms = false;
          bool ignorepatches = false;
          bool reinstall = false;
          bool peruser = false;
          string targetdir = "";
          string command = "";
          string packages = "";

          foreach(string arg in args)
          {
            switch(arg.ToLower())
            {
              case "--ignore-transforms":
                ignoretransforms = true;
              break;
              case "--ignore-patches":
                ignorepatches = true;
              break;
              case "--per-user":
                peruser = true;
              break;
              default:
                if(arg.ToLower().StartsWith("--target-dir"))
                {
                  string[] parts = arg.ToLower().Split('=');
                  if(parts[1] != null)
                    targetdir=parts[1];
                  else
                  {
                    Console.Error.WriteLine("You must specify a directory when using --target-dir");
                    Usage();
                    return 1;
                  }
                }
                else if(command == "")
                  command = arg.ToLower();
                else if(arg.ToLower().StartsWith("-"))
                {
                  Console.Error.WriteLine("Unrecognized option {0}", arg);
                  Usage();
                  return 1;
                }
                else
                  packages += arg + ",";
              break;
            }
          }

          if(command == "")
          {
            Console.Error.WriteLine("Error: no command specified");
            Usage();
            return 1;
          }

          if((command == "install" || command == "remove" || command == "download") && packages == "")
          {
            Console.Error.WriteLine("Error: no packages specified");
            Usage();
            return 1;
          }

          bool success = true;
          switch(command.ToLower())
          {
            case "install":
              foreach(string package in packages.Split(','))
              {
                success = Install(package, ignoretransforms, ignorepatches, peruser, targetdir) && success;
              }
            break;
            case "remove":
              Remove(packages.Split(','));
            break;
            case "download":
              foreach(string package in packages.Split(','))
              {
                success = Download(package) && success;
              }
            break;
            case "show":
              List();
            break;
            case "upgrade":
              Upgrade(packages.Split(','), ignoretransforms, ignorepatches, peruser, targetdir);
            break;
            case "update":
              Update();
            break;
            case "copyright":
              Copyright();
            break;
            default:
              Usage();
            break;
          }
          if(success)
            return 0;
          else
            return 1;
        }
        catch(Exception e)
        {
          Console.Error.WriteLine(
              "Wipt has encountered a serious problem and is unable to continue.  Please report this to SIGWin.");
          Console.Error.WriteLine("");
          Console.Error.Write(e.Message + e.StackTrace);
          return 2;
        }
      }

    private static bool Install(string p, bool ignoretransforms, bool ignorepatches, bool peruser, string targetdir)
    {
      try
      {
        if(p == "")
          return true;
        Version instVersion = null;
        string[] parts = p.Split('=');
        if(parts.Length > 1)
        {
          instVersion = GetProperVersion(parts[1]);
        }
        Product product = Library.GetProduct(parts[0]);
        if(product == null)
        {
          Console.Error.WriteLine("No such product " + parts[0]);
          return false;
        }

        if(instVersion == null ? IsInstalled(product.name) : IsInstalled(product.name, instVersion, instVersion))
        {
          Console.Error.WriteLine(product.name + " is already installed");
          return true;
        }


        if(instVersion == null)
          instVersion = product.stableVersion;

        string URL = "";
        string properties = "";
        Patch[] patches = null;

        if(targetdir == "")
        {
          string temp;
          if((temp = WiptConfig.GetTargetPath()) != null)
          {
            targetdir = temp;
          }
        }
        if(!ignoretransforms && product.transforms.Length > 0)
        {
          properties += "TRANSFORMS=";
          foreach(Transform transform in product.transforms)
          {
            if((transform.minVersion == null || instVersion >= transform.minVersion) && 
                (transform.maxVersion == null || instVersion <= transform.maxVersion))
              properties += transform.URL + ";";
          }
          properties += " ";
        }
        Guid productCode = Guid.Empty;
        patches = product.patches;

        foreach(Package package in product.packages)
        {
          if(package.version == instVersion)
          {
            URL = package.URL;
            productCode = package.productCode;
            break;
          }
        }

        if(URL == "")
        {
          Console.Error.WriteLine("No package listed for specified version of "
              + product.name + ".  Contact the repository maintainer.");
          return false;
        }
        else if(URL.StartsWith("file://"))
        {
          // The Windows Installer system does not support file: URLs.  Don't know why.
          URL = URL.Substring(8);
          URL = URL.Replace("/", "\\");
        }

        WindowsInstaller.MsiInstallState state = WindowsInstaller.QueryProductState(productCode);
        if(state != WindowsInstaller.MsiInstallState.Removed && state != WindowsInstaller.MsiInstallState.Absent 
            && state != WindowsInstaller.MsiInstallState.Unknown)
        {
          // minor upgrade
          properties += "REINSTALL=ALL REINSTALLMODE=vomus ";
        }

        if(targetdir == "")
        {
          targetdir = "\"" + Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "\"";
          Console.Error.WriteLine("Warning: no TARGETDIR specified.  Defaulting to " + targetdir);
        }

        properties += "TARGETDIR=" + targetdir + " REBOOT=R ";
        if(!peruser)
        {
          properties += "ALLUSERS=1 ";
        }

        Console.Write("Installing "+ product.name + "... ");

        WindowsInstaller.SetInternalUI(WindowsInstaller.MsiInstallUILevel.None, IntPtr.Zero);
        /*WindowsInstaller.setProgressHandler(
            new WindowsInstaller.ProgressHandler(
              ProgressHandler));
        WindowsInstaller.setErrorHandler(
            new WindowsInstaller.ErrorHandler(
              ErrorHandler));*/

        uint ret;
        ret = WindowsInstaller.InstallProduct(URL, properties);
        Console.Write("\x08");
        Console.WriteLine(getErrorMessage(ret));


        // Hackish way to prevent reapplication of patches
        foreach(Patch x in patches)
        {
          if(IsPatchApplied(x, productCode))
          {
            x.productCodes = new Guid[1]{Guid.Empty};
          }
        }

        if(ret != 0 && ret != 3010)
          return false;

        if(!(ignorepatches || ApplyPatches(patches, productCode)))
          return false;

        return true;
      }
      catch(WiptException e)
      {
        Console.Error.WriteLine(e.Message);
        return false;
      }
    }

    private static void Remove(string[] packages)
    {
      foreach(string p in packages)
      {
        if(p == "")
          continue;
        Version instVersion = new Version("0.0.0");
        string[] parts = p.Split('=');
        if(parts.Length > 1)
        {
          instVersion = GetProperVersion(parts[1]);
        }
        else
        {
          instVersion = null;
        }
        try
        {
          Product product = Library.GetProduct(parts[0]);
          if(product == null)
          {
            Console.Error.WriteLine("Could not find product " + parts[0]);
            continue;
          }

          int installnum = GetNumberofInstalledVersions(product.upgradeCode);
          if(installnum == 0)
          {
            Console.Error.WriteLine("ERROR: {0} is not installed", product.name);
            continue;
          }
          else if(installnum > 1 && instVersion == null)
          {
            Console.Error.WriteLine("ERROR: More than one version of {0} is installed.  A version must be specified.",
                product.name);
            continue;
          }

          if(instVersion == null ? IsInstalled(product.name) : IsInstalled(product.name, instVersion, instVersion))
          {
            Console.Write("Removing "+ product.name + "... ");

            WindowsInstaller.SetInternalUI(WindowsInstaller.MsiInstallUILevel.None, IntPtr.Zero);
            /*WindowsInstaller.SetProgressHandler(
                new WindowsInstaller.ProgressHandler(
                  ProgressHandler));
            WindowsInstaller.setErrorHandler(
                new WindowsInstaller.ErrorHandler(
                  ErrorHandler));*/

            Guid productCode = GetVersionProductCode(product.upgradeCode, instVersion);

            uint ret = WindowsInstaller.ConfigureProduct(productCode, WindowsInstaller.MsiInstallLevel.Default,
                WindowsInstaller.MsiInstallState.Absent, "REBOOT=R");
            Console.Write("\x08");
            Console.WriteLine(getErrorMessage(ret));
          }
          else
            Console.Error.WriteLine("ERROR: The specified version of {0} is not installed", product.name);
        }
        catch(WiptException e)
        {
          Console.WriteLine(e.Message);
        }
      }
    }

    private static bool Download(string p)
    {
      if(p == "")
        return true;
      Version instVersion = new Version("0.0.0");
      string[] parts = p.Split('=');
      if(parts.Length > 1)
      {
        instVersion = GetProperVersion(parts[1]);
      }
      else
      {
        instVersion = null;
      }
      try
      {
        Product product = Library.GetProduct(parts[0]);
        if(product == null)
        {
          Console.Error.WriteLine("Could not find product " + parts[0]);
          return false;
        }

        string URL = "";
        if(instVersion == null)
          instVersion = product.stableVersion;
        foreach(Package package in product.packages)
        {
          if(package.version == instVersion)
          {
            URL = package.URL;
            break;
          }
        }

        if(URL == "")
        {
          Console.Error.WriteLine("No package listed for specified version of "
              + product.name + ".  Contact the repository maintainer.");
          return false;
        }

        string[] URLParts = URL.Split('/');
        WebClient wc = new WebClient();
        wc.DownloadFile(URL, Environment.CurrentDirectory + "\\" + URLParts[URLParts.Length - 1]);
      }
      catch(Exception e)
      {
        Console.Error.WriteLine(e.Message);
        return false;
      }

      return true;
    }

    public static bool ApplyPatches(Patch[] patches, Guid productCode)
    {
      bool success = true;
      if(patches != null && patches.Length > 0)
      {
        foreach(Patch patch in patches)
        {
          if(patch.productCodes.Length == 0)
          {
            patch.productCodes = new Guid[1];
            patch.productCodes[0] = productCode;
          }
          foreach(Guid code in patch.productCodes)
          {
            if(code == productCode)
            {
              Console.Write("Applying patch "+ patch.name + "... ");
              uint ret = WindowsInstaller.ApplyPatch(patch.URL, "{" + productCode.ToString().ToUpper() + "}", 
                  WindowsInstaller.MsiInstallType.SingleInstance, "");
              Console.Write("\x08");
              Console.WriteLine(getErrorMessage(ret));
              if(ret != 0)
                success = false;
              break;
            }
          }
        }
      }
      return success;
    }

    public static bool IsPatchApplied(Patch patch, Guid productCode)
    {
      Guid[] codes = WindowsInstaller.EnumPatches(productCode);
      foreach(Guid code in codes)
      {
        if(patch.patchCode == code)
          return true;
      }
      
      return false;
    }

    public static void Usage()
    {
      string usage = @"
        Usage: wipt-get [options] <command> <product>[=version][, <product>[=version], ...]
        OPTIONS
        --ignore-patches            Don't apply patches listed in repository
        --ignore-transforms         Don't apply transforms listed in repository
        --per-user                  Perform a per-user install
        --target-dir=DIR            Override the TARGETDIR setting

        COMMANDS
        install                     Install product(s)
        remove                      Remove product(s)
        show                        List all products and packages
        update                      Download package lists from repositories
        upgrade                     Perform small updates and minor upgrades
        download                    Just download the MSI to current directory
        copyright                   Show copyright notice
        ";
      Console.WriteLine(usage);
    }

    public static void Copyright()
    {
      string copyright = @"
        Copyright (c) 2006 Association for Computing Machinery at the 
        University of Illinois at Urbana-Champaign.
        All rights reserved.
        
        Developed by: Special Interest Group for Windows Development
                      ACM@UIUC
                      http://www.acm.uiuc.edu/sigwin
        
        Permission is hereby granted, free of charge, to any person obtaining a 
        copy of this software and associated documentation files (the " + "\"Software\"" + @"),
        to deal with the Software without restriction, including without limitation
        the rights to use, copy, modify, merge, publish, distribute, sublicense,
        and/or sell copies of the Software, and to permit persons to whom the
        Software is furnished to do so, subject to the following conditions:
        
        Redistributions of source code must retain the above copyright notice, this
        list of conditions and the following disclaimers.
        Redistributions in binary form must reproduce the above copyright notice,
        this list of conditions and the following disclaimers in the documentation
        and/or other materials provided with the distribution.
        Neither the names of SIGWin, ACM@UIUC, nor the names of its contributors
        may be used to endorse or promote products derived from this Software
        without specific prior written permission. 
        
        THE SOFTWARE IS PROVIDED " + "\"AS IS\"" + @", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
        IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
        FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
        CONTRIBUTORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
        LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
        FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
        DEALINGS WITH THE SOFTWARE.";
      Console.WriteLine(copyright);
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
        if(e.InnerException != null)
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

    public static void Upgrade(string[] products, bool ignoretransforms, bool ignorepatches, bool peruser,
        string targetdir)
    {
      Product[] list = new Product[0];
      if(products.Length == 1 && products[0] == "")
      {
        list = Library.GetAll();
      }
      else
      {
        foreach(string product in products)
        {
          if(product != "")
          {
            Product p = Library.GetProduct(product);
            if(p == null)
            {
              Console.Error.WriteLine("No such product {0}", product);
            }
            else
            {
              Product[] nl = new Product[list.Length + 1];
              Array.Copy(list, nl, list.Length);
              nl[list.Length] = p;
              list = nl;
            }
          }
        }
      }
      foreach(Product p in list)
      {
        if(IsInstalled(p.name) && !IsInstalled(p.name, p.stableVersion, new Version("99.99.9999")))
        {
          Install(p.name + "=" + p.stableVersion.ToString(), ignoretransforms, ignorepatches, peruser, targetdir);
        }
      }
    }

    public static void List()
    {
      try
      {
        Product[] list = Library.GetAll();
        if(list.Length > 0)
        {
          Array.Sort(list);
          foreach(Product p in list)
          {
            Console.WriteLine("\n" + p.name);
            if(p.packages.Length > 0)
            {
              Array.Sort(p.packages);
              foreach(Package k in p.packages)
              {
                string installstring="";
                if(p.stableVersion == k.version)
                  installstring = "(stable)";
                if(IsInstalled(p.name, k.version, k.version))
                  installstring += "(installed)";
                Console.WriteLine("  v{0} {1}", k.version.ToString(), installstring);
              }
            }
            else
            {
              Console.WriteLine("  [No packages found]");
            }
          }
        }
        else
        {
          Console.Error.WriteLine("No packages found in database.");
        }
      }
      catch(WiptException e)
      {
        Console.WriteLine(e.Message);
      }
    }

    private static Guid GetVersionProductCode(Guid upgradecode, Version version)
    {
      Guid[] products = WindowsInstaller.EnumRelatedProducts(upgradecode);
      foreach(Guid code in products)
      {
        WindowsInstaller.MsiInstallState state = WindowsInstaller.QueryProductState(code);
        if(!(state == WindowsInstaller.MsiInstallState.Removed || state == WindowsInstaller.MsiInstallState.Absent
              || state == WindowsInstaller.MsiInstallState.Unknown))
        {
          if(version == null || version == GetProperVersion(WindowsInstaller.GetProductInfo(code, "VersionString")))
          {
            return code;
          }
        }
      }

      return Guid.Empty;
    }

    private static int GetNumberofInstalledVersions(Guid upgradeCode)
    {
      int products = 0;
      Guid[] codes = WindowsInstaller.EnumRelatedProducts(upgradeCode);
      foreach(Guid code in codes)
      {
        WindowsInstaller.MsiInstallState state = WindowsInstaller.QueryProductState(code);
        if(!(state ==  WindowsInstaller.MsiInstallState.Removed || state == WindowsInstaller.MsiInstallState.Absent
              || state == WindowsInstaller.MsiInstallState.Unknown))
        {
          products++;
        }
      }
      
      return products;
    }

    private static bool IsInstalled(Guid productCode)
    {
      WindowsInstaller.MsiInstallState state = WindowsInstaller.QueryProductState(productCode);
      return !(state ==  WindowsInstaller.MsiInstallState.Removed || state == WindowsInstaller.MsiInstallState.Absent
          || state == WindowsInstaller.MsiInstallState.Unknown);
    }

    private static bool IsInstalled(string product)
    {
      Product p = Library.GetProduct(product);

      if(p != null)
      {
        Guid[] productCodes = WindowsInstaller.EnumRelatedProducts(p.upgradeCode);
        foreach(Guid code in productCodes)
        {
          if(IsInstalled(code))
            return true;
        }
      }

      return false;
    }

    private static bool IsInstalled(string product, Version minVersion, Version maxVersion)
    {
      Product p = Library.GetProduct(product);
      if(p == null)
      {
        return false;
      }

      Guid[] productCodes = WindowsInstaller.EnumRelatedProducts(p.upgradeCode);
      foreach(Guid code in productCodes)
      {
        if(IsInstalled(code))
        {
          Version v = GetProperVersion(WindowsInstaller.GetProductInfo(code, "VersionString"));

          if(v >= minVersion && v <= maxVersion)
          {
            return true;
          }
        }
      }

      return false;
    }

    private static Version GetProperVersion(string version)
    {

      Regex r = new Regex("^[0-9]{1,2}\\.[0-9]{1,2}\\.[0-9]{1,4}");
      Match m = r.Match(version);
      if(m.Success)
        return new Version(m.Value);
      else
        throw new ApplicationException("Invalid Version");
    }

    private static int index = 0;
    private static void ProgressHandler(double progress)
    {
      if(++index >= 4)
        index = 0;
      char[] bob = new char[] {'\\','|','/','-'};
      Console.Write(new char[] {'\x08', bob[index]});
    }

    private static void ErrorHandler(string error)
    {
      Console.WriteLine("\x08");
      Console.WriteLine(error);
      Console.Write("|");
    }

    private static string getErrorMessage(uint ret)
    {
      StringBuilder buffer = new StringBuilder(4096);
      FormatMessage(0x00001000, IntPtr.Zero, ret, 0, buffer, 4096, IntPtr.Zero);
      return buffer.ToString();
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
