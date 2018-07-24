/******************************** Module Header ********************************\
* Module Name:  UnmanagedLibrary.cs
* Project:      CSLoadLibrary
* Copyright (c) Microsoft Corporation.
*
* The source code of UnmanagedLibrary is quoted from Mike Stall's article:
*
* Type-safe Managed wrappers for kernel32!GetProcAddress
* http://blogs.msdn.com/jmstall/archive/2007/01/06/Typesafe-GetProcAddress.aspx
* http://blogs.msdn.com/b/jonathanswift/archive/2006/10/03/dynamically-calling-an-unmanaged-dll-from-.net-_2800_c_23002900_.aspx
*
* This source is subject to the Microsoft Public License.
* See http://www.microsoft.com/en-us/openness/resources/licenses.aspx#MPL.
* All other rights reserved.
*
* THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND,
* EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED
* WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
\*******************************************************************************/

using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using Microsoft.Win32.SafeHandles;

namespace FFXIV.Framework.Common
{
    /// <summary>
    /// Utility class to wrap an unmanaged DLL and be responsible for freeing it.
    /// </summary>
    /// <remarks>
    /// This is a managed wrapper over the native LoadLibrary, GetProcAddress,
    /// and FreeLibrary calls.
    /// </example>
    /// <see cref=
    /// "http://blogs.msdn.com/jmstall/archive/2007/01/06/Typesafe-GetProcAddress.aspx"
    /// />
    public sealed class UnmanagedLibrary : IDisposable
    {
        #region Safe handles and Native imports

        /// <summary>
        /// Native methods
        /// </summary>
        private static class NativeMethod
        {
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            [DllImport("kernel32", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool FreeLibrary(IntPtr hModule);

            [DllImport("kernel32", SetLastError = true, EntryPoint = "GetProcAddress")]
            public static extern IntPtr GetProcAddress(SafeLibraryHandle hModule,
                String procname);

            [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern SafeLibraryHandle LoadLibrary(string fileName);
        }

        /// <summary>
        /// See http://msdn.microsoft.com/msdnmag/issues/05/10/Reliability/
        /// for more about safe handles.
        /// </summary>
        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
        private sealed class SafeLibraryHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            /// <summary>
            /// Create safe library handle
            /// </summary>
            private SafeLibraryHandle() : base(true) { }

            /// <summary>
            /// Release handle
            /// </summary>
            protected override bool ReleaseHandle()
            {
                return NativeMethod.FreeLibrary(handle);
            }
        }

        #endregion Safe handles and Native imports

        /// <summary>
        /// Constructor to load a dll and be responsible for freeing it.
        /// </summary>
        /// <param name="fileName">full path name of dll to load</param>
        /// <exception cref="System.IO.FileNotFoundException">
        /// If fileName can't be found
        /// </exception>
        /// <remarks>
        /// Throws exceptions on failure. Most common failure would be
        /// file-not-found, or that the file is not a loadable image.
        /// </remarks>
        public UnmanagedLibrary(string fileName)
        {
            m_hLibrary = NativeMethod.LoadLibrary(fileName);
            if (m_hLibrary.IsInvalid)
            {
                int hr = Marshal.GetHRForLastWin32Error();
                Marshal.ThrowExceptionForHR(hr);
            }
        }

        /// <summary>
        /// Dynamically lookup a function in the dll via kernel32!GetProcAddress.
        /// </summary>
        /// <param name="functionName">
        /// raw name of the function in the export table.
        /// </param>
        /// <returns>
        /// null if function is not found. Else a delegate to the unmanaged
        /// function.
        /// </returns>
        /// <remarks>
        /// GetProcAddress results are valid as long as the dll is not yet
        /// unloaded. This is very very dangerous to use since you need to
        /// ensure that the dll is not unloaded until after you're done with any
        /// objects implemented by the dll. For example, if you get a delegate
        /// that then gets an IUnknown implemented by this dll, you can not
        /// dispose this library until that IUnknown is collected. Else, you may
        /// free the library and then the CLR may call release on that IUnknown
        /// and it will crash.
        /// </remarks>
        public TDelegate GetUnmanagedFunction<TDelegate>(string functionName)
            where TDelegate : class
        {
            IntPtr p = NativeMethod.GetProcAddress(m_hLibrary, functionName);

            // Failure is a common case, especially for adaptive code.
            if (p == IntPtr.Zero)
            {
                return null;
            }

            Delegate function = Marshal.GetDelegateForFunctionPointer(
                p, typeof(TDelegate));

            // Ideally, we'd just make the constraint on TDelegate be
            // System.Delegate, but compiler error CS0702
            // (constrained can't be System.Delegate)
            // prevents that. So we make the constraint system.object and do the
            // cast from object-->TDelegate.
            object o = function;

            return (TDelegate)o;
        }

        #region IDisposable Members

        // Unmanaged resource. CLR will ensure SafeHandles get freed, without
        // requiring a finalizer on this class.
        private SafeLibraryHandle m_hLibrary;

        /// <summary>
        /// Call FreeLibrary on the unmanaged dll. All function pointers handed
        /// out from this class become invalid after this.
        /// </summary>
        /// <remarks>
        /// This is very dangerous because it suddenly invalidate everything
        /// retrieved from this dll. This includes any functions handed out via
        /// GetProcAddress, and potentially any objects returned from those
        /// functions (which may have an implemention in the dll).
        /// </remarks>
        public void Dispose()
        {
            if (!m_hLibrary.IsClosed)
            {
                m_hLibrary.Close();
            }
        }

        #endregion IDisposable Members
    }
}
