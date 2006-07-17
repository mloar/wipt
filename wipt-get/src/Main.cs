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
              success = Install(package, ignoretransforms, batch) && success;
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
        Version instVersion = null;
        string[] parts = p.Split('=');
        if(parts.Length > 1)
        {
          instVersion = new Version(parts[1]);
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
            if((transform.minVersion == null || instVersion >= transform.minVersion) && 
                (transform.maxVersion == null || instVersion <= transform.maxVersion))
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
          // minor upgrade
          otherprops = "REINSTALL=ALL REINSTALLMODE=vomus";
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
            (transforms != "" ? otherprops + " REBOOT=R ALLUSERS=2 TARGETDIR=" + targetdir + " TRANSFORMS=" + transforms :
             otherprops + " REBOOT=R ALLUSERS=2 TARGETDIR=" + targetdir));
        Console.WriteLine("");

        if(ret != 0)
        {
          Console.Error.WriteLine(
              "Error code {0} returned from installProduct for {1}:"
              , ret, product.name);
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
          instVersion = new Version(parts[1]);
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
            Console.WriteLine("Could not find product " + parts[0]);
            continue;
          }

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
            Console.WriteLine(product.name + " (or the specified version of it) is not installed");
        }
        catch(WiptException e)
        {
          Console.WriteLine(e.Message);
        }
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

    public static void Upgrade(bool ignoretransforms)
    {
      Product[] list = Library.GetAll();
      foreach(Product p in list)
      {
        int k = 0;
        Guid prod;

        while((prod = ApplicationDatabase.findProductByUpgradeCode(p.upgradeCode, k)) != Guid.Empty)
        {
          foreach(Package g in p.packages)
          {
            if(g.productCode == prod)
            {
              if(new Version(ApplicationDatabase.getInstalledVersion(prod)) < g.version)
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

    public static void DistUpgrade(bool ignoretransforms, bool batch)
    {
      try
      {
        Product[] list = Library.GetAll();
        foreach(Product p in list)
        {
          if(IsInstalled(p.name) && !IsInstalled(p.name, p.stableVersion, new Version("99.99.9999")))
          {
            Install(p.name, ignoretransforms, batch);
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
        Product[] list = Library.GetAll();
        Array.Sort(list);
        foreach(Product p in list)
        {
          Console.WriteLine("\n" + p.name);
          Array.Sort(p.packages);
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
      if(version == null || version == new Version(ApplicationDatabase.getInstalledVersion(ret)))
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
    bool installed = false;
    Object obj = Library.GetProduct(product);

    if(obj == null || obj is Patch)
    {
      installed = false;
    }
    else if(obj is Product)
    {
      Product p = (Product)obj;

      int i = 0;
      Guid productCode;
      while((productCode = ApplicationDatabase.findProductByUpgradeCode(p.upgradeCode, i++)) != Guid.Empty)
      {
        InstallState state = ApplicationDatabase.getProductState(productCode);
        if(!(state ==  InstallState.Removed || state == InstallState.Absent || state == InstallState.Unknown))
        {
          installed = true;
        }
      }
    }

    return installed;
  }

  private static bool IsInstalled(string product, Version minVersion, Version maxVersion)
  {
    Object obj = Library.GetProduct(product);
    if(obj == null || !(obj is Product))
    {
      return false;
    }

    Product p = (Product)obj;

    int i = 0;
    Guid productCode;
      while((productCode = ApplicationDatabase.findProductByUpgradeCode(p.upgradeCode, i++)) != Guid.Empty)
      {

        InstallState state = ApplicationDatabase.getProductState(productCode);
        if(!(state == InstallState.Removed || state == InstallState.Absent || state == InstallState.Unknown))
        {

          string[] ver = new string[3];
          ver = ApplicationDatabase.getInstalledVersion(productCode).Split('.');

          Version v = new Version(ver[0], ver[1], ver[2]);
          if(v >= minVersion && v <= maxVersion)
          {
            return true;
          }
        }
      }

      return false;
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
