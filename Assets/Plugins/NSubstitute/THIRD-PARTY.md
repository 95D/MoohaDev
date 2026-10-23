# Third-party test dependencies

Vendored because neither library is published on a Unity package registry. Both are imported as
Editor-only plugins with Auto Reference off, so only the test assembly sees them and no build
includes them. Castle DynamicProxy needs `Reflection.Emit`, which rules out IL2CPP anyway.

| Library | Version | Source | License |
|---|---|---|---|
| NSubstitute | 5.3.0 | `nuget.org/packages/NSubstitute` (`lib/netstandard2.0`) | BSD-3-Clause |
| Castle.Core | 5.1.1 | `nuget.org/packages/Castle.Core` (`lib/netstandard2.0`) | Apache-2.0 |

Castle.Core is NSubstitute's proxy backend, not a direct dependency of the tests.

Castle.Core has **Validate References** off. Every one of its targets references
`System.Diagnostics.EventLog`, which Unity does not ship, so validation refuses to load the
assembly — and the cascade keeps NSubstitute and the test assembly out of the domain too. Only
Castle's event-log trace listener needs that reference, and nothing in the proxy path reaches it.
