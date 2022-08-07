# Code Coverage Summary

<div align="center">

![.NET 6.0](https://img.shields.io/badge/Version-.NET%206.0-informational?style=flat&logo=dotnet)
&nbsp;
![Built With Docker](https://img.shields.io/badge/Built_With-Docker-informational?style=flat&logo=docker)
&nbsp;
[![OpenSSF Best Practices](https://bestpractices.coreinfrastructure.org/projects/6292/badge)](https://bestpractices.coreinfrastructure.org/projects/6292)
&nbsp;
[![CI Build](https://github.com/irongut/CodeCoverageSummary/actions/workflows/ci-build.yml/badge.svg)](https://github.com/irongut/CodeCoverageSummary/actions/workflows/ci-build.yml)
&nbsp;
[![CodeQL Scan](https://github.com/irongut/CodeCoverageSummary/actions/workflows/codeql-scan.yml/badge.svg)](https://github.com/irongut/CodeCoverageSummary/actions/workflows/codeql-scan.yml)

</div>

A GitHub Action that reads Cobertura format code coverage files from your test suite and outputs a text or markdown summary. This summary can be posted as a Pull Request comment or included in Release Notes by other actions to give you an immediate insight into the health of your code without using a third-party site.

Code Coverage Summary is designed for use with any test framework that outputs coverage in Cobertura XML format including [Coverlet](https://github.com/coverlet-coverage/coverlet), [gcovr](https://github.com/gcovr/gcovr), [simplecov](https://github.com/simplecov-ruby/simplecov) and [MATLAB](https://uk.mathworks.com/help/matlab/ref/matlab.unittest.plugins.codecoverageplugin-class.html). See the [FAQ](https://github.com/irongut/CodeCoverageSummary/wiki/Frequently-Asked-Questions#which-testing-tools-does-ccs-work-with) for more details. If it doesn't work with your tooling please [open an issue][new-issue] to discuss the problem.

Code Coverage Summary is compatible with [StepSecurity Secure Workflows](https://github.com/step-security/secure-workflows) and uses a Docker image that is cryptographically signed using [Sigstore](https://www.sigstore.dev/). For instructions how to verify the Docker image please see the [Wiki](https://github.com/irongut/CodeCoverageSummary/wiki/Verify-the-Docker-Image).

**As a Docker based action Code Coverage Summary requires a Linux runner.**


## Inputs

### `filename`
**Required**

A comma separated list of code coverage files to analyse. Also supports using glob patterns to match multiple files. If there are any spaces in a path or filename this value must be in quotes.


### `badge`

Include a badge reporting the Line Rate coverage in the output using [shields.io](https://shields.io/) - `true` or `false` (default).

Line Rate | Badge
--------- | -----
less than lower threshold (50%) | ![Code Coverage](https://img.shields.io/badge/Code%20Coverage-45%25-critical?style=flat)
between thresholds (50% - 74%) | ![Code Coverage](https://img.shields.io/badge/Code%20Coverage-65%25-yellow?style=flat)
equal or greater than upper threshold (75%) | ![Code Coverage](https://img.shields.io/badge/Code%20Coverage-83%25-success?style=flat)

See [`thresholds`](#thresholds) to change these values.


### `fail_below_min`

Fail the workflow if the overall Line Rate is below lower threshold - `true` or `false` (default). The default lower threshold is 50%, see [`thresholds`](#thresholds).


### `format`

Output Format - `markdown` or `text` (default).


### `hide_branch_rate`

Hide Branch Rate metrics in the output - `true` or `false` (default).


### `hide_complexity`

Hide Complexity metrics in the output - `true` or `false` (default).


### `indicators`

Include health indicators in the output - `true` (default) or `false`.

Line Rate | Indicator
--------- | ---------
less than lower threshold (50%) | ❌
between thresholds (50% - 74%) | ➖
equal or greater than upper threshold (75%) | ✔

See [`thresholds`](#thresholds) to change these values.


### `output`

Output Type - `console` (default), `file` or `both`.

`console` will output the coverage summary to the GitHub Action log.

`file` will output the coverage summary to `code-coverage-results.txt` for text or `code-coverage-results.md` for markdown format in the workflow's working directory.

`both` will output the coverage summary to the Action log and a file as above.


### `thresholds`

Lower and upper threshold percentages for badge and health indicators, lower threshold can also be used to fail the action. Separate the values with a space and enclose them in quotes; default `'50 75'`.


## Outputs

### Text Example
```
https://img.shields.io/badge/Code%20Coverage-83%25-success?style=flat

Company.Example: Line Rate = 83%, Branch Rate = 69%, Complexity = 671, ✔
Company.Example.Library: Line Rate = 27%, Branch Rate = 100%, Complexity = 11, ❌
Summary: Line Rate = 83% (1212 / 1460), Branch Rate = 69% (262 / 378), Complexity = 682, ✔
Minimum allowed line rate is 50%
```


### Markdown Example

> ![Code Coverage](https://img.shields.io/badge/Code%20Coverage-83%25-success?style=flat)
> 
> Package | Line Rate | Branch Rate | Complexity | Health
> -------- | --------- | ----------- | ---------- | ------
> Company.Example | 83% | 69% | 671 | ✔
> Company.Example.Library | 27% | 100% | 11 | ❌
> **Summary** | **83%** (1212 / 1460) | **69%** (262 / 378) | 682 | ✔
> 
> _Minimum allowed line rate is `50%`_


## Usage

```yaml
name: Code Coverage Summary Report
uses: irongut/CodeCoverageSummary@v1.3.0
with:
  filename: coverage.cobertura.xml
```


### .Net Workflow Example

```yaml
name: .Net 6 CI Build

on:
  push:
    branches: [ master ]
  pull_request:
    branches: [ master ]

jobs:
  build:
    runs-on: ubuntu-latest
    name: CI Build
    steps:
    - name: Checkout
      uses: actions/checkout@v2

    - name: Setup .NET
      uses: actions/setup-dotnet@v1
      with:
        dotnet-version: 6.0.x

    - name: Restore Dependencies
      run: dotnet restore src/Example.sln

    - name: Build
      run: dotnet build src/Example.sln --configuration Release --no-restore

    - name: Test
      run: dotnet test src/Example.sln --configuration Release --no-build --verbosity normal --collect:"XPlat Code Coverage" --results-directory ./coverage

    - name: Code Coverage Report
      uses: irongut/CodeCoverageSummary@v1.3.0
      with:
        filename: coverage/**/coverage.cobertura.xml
        badge: true
        fail_below_min: true
        format: markdown
        hide_branch_rate: false
        hide_complexity: true
        indicators: true
        output: both
        thresholds: '60 80'

    - name: Add Coverage PR Comment
      uses: marocchino/sticky-pull-request-comment@v2
      if: github.event_name == 'pull_request'
      with:
        recreate: true
        path: code-coverage-results.md
```


## Version Numbers

Version numbers will be assigned according to the [Semantic Versioning](https://semver.org/) scheme.
This means, given a version number MAJOR.MINOR.PATCH, we will increment the:

1. MAJOR version when we make incompatible API changes
2. MINOR version when we add functionality in a backwards compatible manner
3. PATCH version when we make backwards compatible bug fixes


## Contributing

### Report Bugs

Please make sure the bug is not already reported by searching existing [issues].

If you're unable to find an existing issue addressing the problem please [open a new one][new-issue]. Be sure to include a title and clear description, as much relevant information as possible, a workflow sample and any logs demonstrating the problem.


### Suggest an Enhancement

Please [open a new issue][new-issue].


### Submit a Pull Request

Discuss your idea first, so that your changes have a good chance of being merged in.

Submit your pull request against the `master` branch.

Pull requests that include documentation and relevant updates to README.md are merged faster, because you won't have to wait for somebody else to complete your contribution.


## License

Code Coverage Summary is available under the MIT license, see the [LICENSE](LICENSE) file for more info.

[issues]: https://github.com/irongut/CodeCoverageSummary/issues
[new-issue]: https://github.com/irongut/CodeCoverageSummary/issues/new
