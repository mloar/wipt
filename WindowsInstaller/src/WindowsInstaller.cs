/*
 *  Copyright (c) 2006 Association for Computing Machinery at the 
 *  University of Illinois at Urbana-Champaign.
 *  All rights reserved.
 * 
 *  Developed by: Special Interest Group for Windows Development
 *                ACM@UIUC
 *                http://www.acm.uiuc.edu/sigwin
 *
 *  Permission is hereby granted, free of intge, to any person obtaining a 
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
using System.Runtime.InteropServices;
using System.Text;

namespace ACM.Wipt
{
  /// <remarks>This implements a partial interface to the Windows Installer.</remarks>
  public abstract class WindowsInstaller
  {
    public delegate int MsiExternalUIHandler(IntPtr pvContext, uint iMessageType, string szMessage);

    [DllImport("msi.dll")]
      private static extern uint MsiEnumRelatedProducts(
          string lpUpgradeCode, uint dwReserved, uint lProductIndex, StringBuilder lpProductBuf);

    [DllImport("msi.dll")]
      private static extern uint MsiInstallProduct(string szPackagePath, string szCommandLine);

    [DllImport("msi.dll")]
      private static extern uint MsiApplyPatch(string szPatchPackage, string szInstallPackage,
          int eInstallType, string szCommandLine);

    [DllImport("msi.dll")]
      private static extern uint MsiConfigureProductEx(
          string szProduct, int iInstallLevel, int eInstallState, string szCommandLine);

    [DllImport("msi.dll")]
      private static extern uint MsiEnumPatches(
          string szProduct, uint iPatchIndex, StringBuilder lpPatchBuf, StringBuilder lpTransformsBuf, 
          ref uint pcchTransformsBuf);

    [DllImport("msi.dll")]
      private static extern uint MsiVerifyPackage(string szPackagePath);

    [DllImport("msi.dll")]
      private static extern uint MsiSetInternalUI(int dwUILevel, IntPtr phWnd);

    [DllImport("msi.dll")]
      private static extern uint MsiSetExternalUI(MsiExternalUIHandler puiHandler, uint dwMessageFilter,
          IntPtr pvContext);

    [DllImport("msi.dll")]
      private static extern uint MsiQueryProductState(string szProduct);

    [DllImport("msi.dll")]
      private static extern uint MsiGetProductInfo(string szProduct, string szProperty, StringBuilder lpValueBuf,
          ref uint pcchValueBuf);

    [DllImport("msi.dll")]
      private static extern uint MsiOpenDatabase(string szDatabasePath, IntPtr szPersist, out IntPtr phDatabase);

    [DllImport("msi.dll")]
      private static extern uint MsiCloseHandle(IntPtr hDatabase);

    [DllImport("msi.dll")]
      private static extern uint MsiDatabaseOpenView(IntPtr hDatabase, string szQuery, out IntPtr phView);

    [DllImport("msi.dll")]
      private static extern IntPtr MsiCreateRecord(uint cParams);

    [DllImport("msi.dll")]
      private static extern uint MsiRecordGetString(IntPtr hRecord, uint iField, StringBuilder szValueBuf,
          ref uint pcchValueBuf);

    [DllImport("msi.dll")]
      private static extern uint MsiRecordSetString(IntPtr hRecord, uint iField, string szValue);

    [DllImport("msi.dll")]
      private static extern uint MsiViewExecute(IntPtr hView, IntPtr hRecord);

    [DllImport("msi.dll")]
      private static extern uint MsiViewFetch(IntPtr hView, out IntPtr phRecord);

    /// <summary>Install types for ApplyPatch.</summary>
    public enum MsiInstallType
    {
      /// <summary>For normal patch application.</summary>
      Default,
      /// <summary>For administrative installations.</summary>
      NetworkImage,
      /// <summary>For application to a single product.</summary>
      SingleInstance
    };

    /// <summary>Install levels for ConfigureProduct.</summary>
    public enum MsiInstallLevel
    {
    /// <summary>Blah</summary>
      Default,
    /// <summary>Blah</summary>
      Minimum,
    /// <summary>Blah</summary>
      Maximum
    };

    /// <summary>Install states for ConfigureProduct.</summary>
    public enum MsiInstallState
    {
    /// <summary>Blah</summary>
      NotUsed       = -7,
    /// <summary>Blah</summary>
      BadConfig     = -6,
    /// <summary>Blah</summary>
      Incomplete    = -5,
    /// <summary>Blah</summary>
      SourceAbsent  = -4,
    /// <summary>Blah</summary>
      MoreData      = -3,
    /// <summary>Blah</summary>
      InvalidArg    = -2,
    /// <summary>Blah</summary>
      Unknown       = -1,
    /// <summary>Blah</summary>
      Broken        = 0,
    /// <summary>Blah</summary>
      Advertised    = 1,
    /// <summary>Blah</summary>
      Removed       = 1,
    /// <summary>Blah</summary>
      Absent        = 2,
    /// <summary>Blah</summary>
      Local         = 3,
    /// <summary>Blah</summary>
      Source        = 4,
    /// <summary>Blah</summary>
      Default       = 5
    };

    /// <summary>Blah</summary>
    public enum MsiInstallUILevel
    {
      /// <summary>Blah</summary>
      NoChange,
      /// <summary>Blah</summary>
      Default,
      /// <summary>Blah</summary>
      None,
      /// <summary>Blah</summary>
      Basic,
      /// <summary>Blah</summary>
      Reduced,
      /// <summary>Blah</summary>
      Full,
      /// <summary>Blah</summary>
      EndDialog,
      /// <summary>Blah</summary>
      ProgressOnly,
      /// <summary>Blah</summary>
      HideCancel,
      /// <summary>Blah</summary>
      SourceResOnly
    };

    /// <summary>Blah</summary>
    public enum MsiInstallMessage
    {
      FatalExit       = 0x00000000,
      Error           = 0x01000000,
      Warning         = 0x02000000,
      User            = 0x03000000,
      Info            = 0x04000000,
      FilesInUse      = 0x05000000,
      ResolveSource   = 0x06000000,
      OutOfDiskSpace  = 0x07000000,
      ActionState     = 0x08000000,
      ActionData      = 0x09000000,
      Progress        = 0x0A000000,
      CommonData      = 0x0B000000,
      Initialize      = 0x0C000000,
      Terminate       = 0x0D000000,
      ShowDialog      = 0x0E000000,
      RMFilesInUse    = 0x19000000
    };

    public enum MsiInstallLogMode
    {
      FatalExit       = (1 << (MsiInstallMessage.FatalExit >> 24)),
      Error           = (1 << (MsiInstallMessage.Error >> 24)),
      Warning         = (1 << (MsiInstallMessage.Warning >> 24)),
      User            = (1 << (MsiInstallMessage.User >> 24)),
      Info            = (1 << (MsiInstallMessage.Info >> 24)),
      FilesInUse      = (1 << (MsiInstallMessage.FilesInUse >> 24)),
      ResolveSource   = (1 << (MsiInstallMessage.ResolveSource >> 24)),
      OutOfDiskSpace  = (1 << (MsiInstallMessage.OutOfDiskSpace >> 24)),
      ActionState     = (1 << (MsiInstallMessage.ActionState >> 24)),
      ActionData      = (1 << (MsiInstallMessage.ActionData >> 24)),
      Progress        = (1 << (MsiInstallMessage.Progress >> 24)),
      CommonData      = (1 << (MsiInstallMessage.CommonData >> 24)),
      Initialize      = (1 << (MsiInstallMessage.Initialize >> 24)),
      Terminate       = (1 << (MsiInstallMessage.Terminate >> 24)),
      ShowDialog      = (1 << (MsiInstallMessage.ShowDialog >> 24)),
      RMFilesInUse    = (1 << (MsiInstallMessage.RMFilesInUse >> 24)),
    };

    /// <summary>Enumerates known products with a certain upgrade code.</summary>
    /// <param name="UpgradeCode">
    /// The UpgradeCode for which products are to be enumerated.
    /// </param>
    public static Guid[] EnumRelatedProducts(Guid UpgradeCode)
    {
      Guid[] guids = new Guid[0];

      uint ret;
      uint i = 0;
      StringBuilder buffer = new StringBuilder(38);
      while((ret = MsiEnumRelatedProducts("{" + UpgradeCode.ToString().ToUpper() + "}", 0, i, buffer)) == 0)
      {
        Guid[] temp = new Guid[i + 1];
        Array.Copy(guids, temp, i);
        temp[i] = new Guid(buffer.ToString());
        guids = temp;
        i++;
      }
      if(ret != 259)
      {
        throw new ApplicationException("?");
      }

      return guids;
    }

    /// <summary>Installs a product from an MSI package.</summary>
    /// <param name="PackagePath">
    /// Path to the MSI package.
    /// </param>
    /// <param name="CommandLine">
    /// Command line for installation.
    /// </param>
    public static uint InstallProduct(string PackagePath, string CommandLine)
    {
      uint ret = 0;
      try
      {
        ret = MsiInstallProduct(PackagePath, CommandLine);
      }
      catch(Exception e)
      {
        Console.WriteLine("An internal error occurred in Wipt.  The installation may still be in process.");
      }

      return ret;
    }

    /// <summary>Applies a patch to a product.</summary>
    /// <param name="PatchPackage">
    /// Path to the MSP package.
    /// </param>
    /// <param name="InstallPackage">
    /// If InstallType is set to MsiInstallType.NetworkImage, this parameter is a null-terminated string that specifies
    /// a path to the product that is to be patched.  If InstallType is set to MsiInstallType.SingleInstance, the
    /// installer applies the patch to the product whose Product Code is specified in this parameter.
    /// </param>
    /// <param name="InstallType">
    /// One of the values in MsiInstallType.
    /// </param>
    /// <param name="CommandLine">
    /// The command line for the patch application.
    /// </param>
    public static uint ApplyPatch(string PatchPackage, string InstallPackage, MsiInstallType InstallType,
        string CommandLine)
    {
      if(InstallPackage == null)
      {
        if(InstallType == MsiInstallType.NetworkImage)
          throw new ArgumentException("A path must be passed in InstallPackage if InstallType is NetworkImage.");

        if(InstallType == MsiInstallType.SingleInstance)
          throw new ArgumentException(
              "A product code must be passed in InstallPackage if InstallType is SingleInstance.");
      }
      else if(InstallType == MsiInstallType.Default)
        throw new ArgumentException("InstallPackage must be null if InstallType is Default.");

      return MsiApplyPatch(PatchPackage, InstallPackage, (int)InstallType, CommandLine);
    }

    /// <summary>Blah</summary>
    public static uint ConfigureProduct(
        Guid ProductCode, MsiInstallLevel InstallLevel, MsiInstallState InstallState, string CommandLine)
    {
      return MsiConfigureProductEx("{" + ProductCode.ToString().ToUpper() + "}", (int)InstallLevel, 
          (int)InstallState, CommandLine);
    }

    /// <summary>Blah</summary>
    public static Guid[] EnumPatches(Guid ProductCode)
    {
      Guid[] guids = new Guid[0];

      uint ret;
      uint i = 0;
      uint datasize = 4096;
      StringBuilder buffer = new StringBuilder(38);
      StringBuilder useless = new StringBuilder((int)datasize);
      while((ret = MsiEnumPatches("{" + ProductCode.ToString().ToUpper() + "}", i, buffer, useless, ref datasize)) == 0)
      {
        Guid[] temp = new Guid[i + 1];
        Array.Copy(guids, temp, i);
        temp[i] = new Guid(buffer.ToString());
        guids = temp;
        i++;
      }
      if(ret != 259)
      {
        throw new ApplicationException("?");
      }

      return guids;
    }

    /// <summary>Blah</summary>
    public static uint VerifyPackage(string PackagePath)
    {
      return MsiVerifyPackage(PackagePath);
    }

    /// <summary>Blah</summary>
    public static uint SetInternalUI(MsiInstallUILevel InstallUILevel, IntPtr phWnd)
    {
      return MsiSetInternalUI((int)InstallUILevel, phWnd);
    }

    /// <summary>Blah</summary>
    public static uint SetExternalUI(MsiExternalUIHandler ExternalUIHandler, MsiInstallLogMode MessageFilter,
        IntPtr Context)
    {
      return MsiSetExternalUI(ExternalUIHandler, (uint)MessageFilter, Context);
    }

    /// <summary>Blah</summary>
    public static MsiInstallState QueryProductState(Guid ProductCode)
    {
      return (MsiInstallState)MsiQueryProductState("{" + ProductCode.ToString().ToUpper() + "}");
    }

    /// <summary>Blah</summary>
    public static string GetProductInfo(Guid ProductCode, string Property)
    {
      uint datasize = 4096;
      StringBuilder buffer = new StringBuilder((int)datasize);

      MsiGetProductInfo("{" + ProductCode.ToString().ToUpper() + "}", Property, buffer, ref datasize);
      return buffer.ToString();
    }

    public class MsiDatabase : IDisposable
    {
      IntPtr msiHandle;

      public MsiDatabase(string PackagePath)
      {
        if(MsiOpenDatabase(PackagePath, IntPtr.Zero, out msiHandle) != 0)
        {
          throw new ApplicationException("Could not open MSI database.");
        }
      }

      public void Dispose()
      {
        MsiCloseHandle(msiHandle);
        msiHandle = IntPtr.Zero;
      }

      private string GetProperty(string Property)
      {
        IntPtr view;
        if(MsiDatabaseOpenView(msiHandle, "SELECT `Value` FROM `Property` WHERE `Property`.`Property`= ?", out view)
            != 0)
        {
          return null;
        }

        IntPtr param;
        param = MsiCreateRecord(1);

        if(MsiRecordSetString(param, 1, Property) != 0)
        {
          MsiCloseHandle(param);
          MsiCloseHandle(view);
          return null;
        }

        if(MsiViewExecute(view, param) != 0)
        {
          MsiCloseHandle(param);
          MsiCloseHandle(view);
          return null;
        }

        IntPtr rec;
        if(MsiViewFetch(view, out rec) != 0)
        {
          MsiCloseHandle(param);
          MsiCloseHandle(view);
          return null;
        }

        uint datasize = 4096;
        StringBuilder buffer = new StringBuilder((int)datasize);
        if(MsiRecordGetString(rec, 1, buffer, ref datasize) != 0)
        {
          MsiCloseHandle(rec);
          MsiCloseHandle(param);
          MsiCloseHandle(view);
          return null;
        }

        MsiCloseHandle(rec);
        MsiCloseHandle(param);
        MsiCloseHandle(view);

        return buffer.ToString();

      }

      public Guid ProductCode
      {
        get
        {
          return new Guid(GetProperty("ProductCode"));
        }
      }

      public string ProductName
      {
        get
        {
          return GetProperty("ProductName");
        }
      }

      public string ProductVersion
      {
        get
        {
          return GetProperty("ProductVersion");
        }
      }

      public string Manufacturer
      {
        get
        {
          return GetProperty("Manufacturer");
        }
      }

      public string SupportURL
      {
        get
        {
          return GetProperty("ARPHELPLINK");
        }
      }

      public Guid UpgradeCode
      {
        get
        {
          return new Guid(GetProperty("UpgradeCode"));
        }
      }
    }
  }
}
