# Third-Party Notices

Edge Workspace Manager is licensed separately under the Edge Workspace Manager
Community License 1.0 in the `LICENSE` file. The notices below apply only to
the identified third-party components and do not change the license of Edge
Workspace Manager.

## Microsoft Edge WebView2 SDK

- Component: `Microsoft.Web.WebView2`
- Package version: `1.0.2903.40`
- Copyright: Copyright (C) Microsoft Corporation. All rights reserved.
- License source: <https://www.nuget.org/packages/Microsoft.Web.WebView2/1.0.2903.40/License>
- Package source: <https://www.nuget.org/packages/Microsoft.Web.WebView2/1.0.2903.40>

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice,
   this list of conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.
3. The name of Microsoft Corporation, or the names of its contributors, may
   not be used to endorse or promote products derived from this software
   without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

The WebView2 SDK package also contains third-party material. Microsoft makes
certain corresponding open-source code available from
<https://3rdpartysource.microsoft.com>. The package notice identifies:

- Antlr3.Runtime 3.5.2-rc1 — BSD 3-Clause — Copyright (c) 2011 The ANTLR Project.
- StringTemplate4 4.0.9-rc1 — BSD 3-Clause — Copyright (c) 2011 The ANTLR Project.

Those components are provided under the BSD 3-Clause license reproduced above.
The names of their copyright holders and contributors may not be used to
endorse or promote derived products without prior written permission.

## Microsoft Edge WebView2 Runtime

The Evergreen WebView2 Runtime is installed and serviced separately by
Microsoft; it is not relicensed under the Edge Workspace Manager Community
License. Use and redistribution are subject to the Microsoft WebView2 Runtime
terms.

- Runtime distribution documentation:
  <https://learn.microsoft.com/microsoft-edge/webview2/concepts/distribution>
- Runtime download and terms:
  <https://developer.microsoft.com/microsoft-edge/webview2/>

## Microsoft .NET Runtime and Windows Desktop Runtime

Release packages are published as .NET 8 self-contained applications and
therefore include Microsoft .NET runtime components.

- Copyright: Copyright (c) .NET Foundation and Contributors.
- License: MIT License.
- License source: <https://github.com/dotnet/runtime/blob/main/LICENSE.TXT>
- Third-party notices: <https://github.com/dotnet/runtime/blob/main/THIRD-PARTY-NOTICES.TXT>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

The .NET runtime includes additional third-party components. Their complete
notices are maintained by the .NET project at the Third-party notices link
above and remain applicable to the copies included in self-contained releases.
The .NET self-contained publish process also places its exact `LICENSE.TXT` and
`THIRD-PARTY-NOTICES.TXT` files beside the application executable; the release
workflow includes both files in the distributed ZIP package.

## GitHub Actions

The release workflow references GitHub Actions to build and publish releases.
These actions run only in the build environment and are not bundled into the
application package. Their licenses remain available in their source
repositories:

- `actions/checkout`: <https://github.com/actions/checkout>
- `actions/setup-dotnet`: <https://github.com/actions/setup-dotnet>
- `softprops/action-gh-release`: <https://github.com/softprops/action-gh-release>
