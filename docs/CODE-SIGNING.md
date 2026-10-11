# Code signing

Nothing Helpers ships is signed yet. This page says why that matters, what the options are, and the plan. Researched 11 October 2026; prices and rules change, so check the sources before acting.

## Why it matters

- **SmartScreen** warns about unsigned apps: "Windows protected your PC", with **More info** and **Run anyway**.
- **Smart App Control** blocks unsigned code outright, with no way past it except turning it off. It checks every DLL, not just the installer. When Microsoft's cloud has no verdict on a file, it allows anything signed by a trusted certificate authority. It blocked development builds on the second PC.
- No certificate gives warning-free installs from day one. Since 2024 even EV certificates build SmartScreen reputation over weeks of clean installs.

## The options

| | Cost | Who can use it | Fits the release workflow | Catch |
|---|---|---|---|---|
| [SignPath Foundation](https://signpath.org) | Free | Open-source projects with "a certain verifiable reputation". In October 2026 it refused projects with 1,356 downloads and 24 stars as not visible enough. | Yes: a GitHub Action, with a manual approval for each release | Signs only the project's own files. Needs a "Code signing policy" section on the home and download pages. |
| [Azure Artifact Signing](https://learn.microsoft.com/en-us/azure/artifact-signing/faq) (was Trusted Signing) | £7.54 a month | Organisations in the UK. Individuals only in the US and Canada. | Yes: an official action, signing in through OIDC | Needs a paid Azure subscription and a company |
| [Certum Open Source](https://shop.certum.eu/open-source-code-signing-on-simplysign.html) | From €49, for up to 460 days | Individuals, with ID, a utility bill and a project page that shows they run it | By hand: the SimplySign app and a phone code. Headless signing needs unofficial tools that hold the phone code's secret. | Revoked if used for commercial software |

## The plan

**Decided 11 October 2026:** ship unsigned for now. From 0.1.1, every release file has a build attestation instead: a record, signed by GitHub, that the file was built from this repository by its release workflow. Anyone can check one with `gh attestation verify <file> --repo drhumphrey/helpers`, and they're listed at [github.com/drhumphrey/helpers/attestations](https://github.com/drhumphrey/helpers/attestations). It proves where a file came from; it doesn't stop the Windows warnings.

1. **Now:** nothing to buy. Get ready: two-factor sign-in on GitHub (SignPath requires it), and the product name is already "Helpers", which SignPath requires to match.
2. **When Helpers has visible use** (stars, downloads, issues from people other than the maintainer): apply to SignPath at [signpath.org/apply](https://signpath.org/apply). Once accepted, add the policy section below to the README and the release notes, and the signing steps to the release workflow.
3. **If signing is wanted sooner:** a Certum certificate, used by hand on the maintainer's PC for each release.

The choice is Dave's.

## Two things any route must handle

- **Inno Setup's temporary file.** When the installer runs, it extracts a `.tmp` copy of itself and runs that. Only Inno's `SignTool` or `SignedUninstaller` settings get it signed, and unsigned, Smart App Control blocks it ([Inno Setup's maintainer, August 2026](https://groups.google.com/g/innosetup/c/7RpEFVoaHZI)). With SignPath, an untested route: set `SignedUninstaller=yes`, let the first compile write `uninst-*.e32` to `SignedUninstallerDir`, sign that file with the app's own files, put it back, and compile again ([Inno Setup help](https://jrsoftware.org/ishelp/topic_setup_signeduninstaller.htm)). Azure's signing tool can be Inno's `SignTool` directly, which signs the installer, its temporary file and the uninstaller in one go.
- **Other projects' files.** In a build measured on the second PC, 73 of the 302 program files were unsigned, mostly libraries: Avalonia, NAudio, LLamaSharp and llama.cpp, sherpa-onnx, ONNX Runtime, Markdig, FlaUI and the Anthropic SDK, plus the app's own. Smart App Control loads an unsigned library only if Microsoft's cloud already knows it, and SignPath won't sign other projects' files. So signing helps SmartScreen most; Smart App Control may still stop a library it doesn't know.

## How SignPath plugs into the release

The action is `signpath/github-action-submit-signing-request@v3`. It takes `api-token`, `organization-id`, `project-slug`, `signing-policy-slug` and the `github-artifact-id` from an `actions/upload-artifact` step before it. Every job must run on GitHub's own runners. The usual pattern: one request signs the app's files, Inno Setup builds the installer from them, and a second request signs the installer. Each request needs its own approval. ([SignPath's GitHub guide](https://docs.signpath.io/trusted-build-systems/github))

## The policy text SignPath needs

On the home page and the download page, once accepted:

> **Code signing policy**
>
> Free code signing provided by [SignPath.io](https://signpath.io), certificate by [SignPath Foundation](https://signpath.org).
>
> Committers and reviewers: [members](https://github.com/drhumphrey/helpers/graphs/contributors). Approvers: [owner](https://github.com/drhumphrey).
>
> Privacy: [docs/PRIVACY.md](PRIVACY.md). This program sends nothing to other computers unless the person using it asks it to.

SignPath's other rules, from its [terms](https://signpath.org/terms): an OSI licence with no proprietary parts, actively maintained, already released in the form to be signed, an installer that can uninstall, and the same product name and version across a build. Helpers meets these today.

## Not checked

What SignPath's application form asks, how long it takes, whether one person can hold every role, whether it would sign Inno's uninstaller, and whether Microsoft's cloud already knows the libraries Helpers ships.
