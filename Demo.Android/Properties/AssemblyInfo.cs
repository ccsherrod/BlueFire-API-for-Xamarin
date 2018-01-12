﻿using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Android.App;

using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]

[assembly: AssemblyTrademark("BlueFire")]
[assembly: AssemblyCompany("BlueFire LLC")]
[assembly: AssemblyProduct("BlueFire API Demo")] // this is used for the app folder name
[assembly: AssemblyDescription("BlueFire API Demo Application")]
[assembly: AssemblyCopyright("Copyright © BlueFire LLC 2014. All rights reserved")]

[assembly: AssemblyTitle("BlueFire API Demo")] // this needs to match the iOS version

[assembly: AssemblyCulture("")]
[assembly: AssemblyConfiguration("")]

//[assembly: AssemblyVersion("0.0.0")] // must use AssemblyFileVersion for compatibility with Windows UWP
[assembly: AssemblyFileVersion("3.4.0")] // this needs to match the versionName in the Manifest and the iOS version

#if DEBUG
[assembly: Application(Debuggable = true)]
#else
[assembly: Application(Debuggable = false)]
#endif
