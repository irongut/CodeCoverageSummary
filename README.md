# Code Coverage Summary

A GitHub Action that reads Cobertura format code coverage files from your test suite and outputs a text or markdown summary. This summary can be posted as a Pull Request comment or included in Release Notes by other actions to give you an immediate insight into the health of your code without using a third-party site.

Code Coverage Summary is designed for use with [Coverlet](https://github.com/coverlet-coverage/coverlet) and [gcovr](https://github.com/gcovr/gcovr) but it should work with any test framework that outputs coverage in Cobertura format.

As a Docker based action Code Coverage Summary requires a Linux runner, see [Types of Action](https://docs.github.com/en/actions/creating-actions/about-custom-actions#types-of-actions). If you need to build with a Windows or MacOS runner a workaround would be to upload the coverage file as an artifact and use a separate job with a Linux runner to generate the summary.

## Inputs

#### `filename`
**Required**

Code coverage file to analyse.

Note: Coverlet creates the coverage file in a random named directory (guid) so you need to copy it to a predictable path before running this Action, see the [.Net 5 Workflow Example](#net-5-workflow-example) below.

#### `badge`

Include a badge reporting the Line Rate coverage in the output using [shields.io](https://shields.io/) - `true` or `false` (default).

Line Rate | Badge
--------- | ---------
less than lower threshold (50%) | ![Code Coverage](https://img.shields.io/badge/Code%20Coverage-45%25-critical?style=flat)
between thresholds (50% - 74%) | ![Code Coverage](https://img.shields.io/badge/Code%20Coverage-65%25-yellow?style=flat)
equal or greater than upper threshold (75%) | ![Code Coverage](https://img.shields.io/badge/Code%20Coverage-83%25-success?style=flat)

See [`thresholds`](#thresholds) to change these values.

#### `fail_below_min`
**v1.1.0-beta only**

Fail the workflow if the overall Line Rate is below lower threshold - `true` or `false` (default). The default lower threshold is 50%, see [`thresholds`](#thresholds).

#### `format`

Output Format - `markdown` or `text` (default).

#### `indicators`
**v1.1.0-beta only**

Include health indicators in the output - `true` (default) or `false`.

Line Rate | Indicator
--------- | ---------
less than lower threshold (50%) | ❌
between thresholds (50% - 74%) | ➖
equal or greater than upper threshold (75%) | ✔

See [`thresholds`](#thresholds) to change these values.

#### `output`

Output Type - `console` (default), `file` or `both`.

`console` will output the coverage summary to the GitHub Action log.

`file` will output the coverage summary to `code-coverage-results.txt` for text or `code-coverage-results.md` for markdown format in the workflow's working directory.

`both` will output the coverage summary to the Action log and a file as above.

#### `thresholds`
**v1.1.0-beta only**

Lower and upper threshold percentages for badge and health indicators, lower threshold can also be used to fail the action. Separate the values with a space and enclose them in quotes; default `'50 75'`.

## Outputs

#### Text Example
```
https://img.shields.io/badge/Code%20Coverage-83%25-success?style=flat

Company.Example: Line Rate = 83%, Branch Rate = 69%, Complexity = 671, ✔
Company.Example.Library: Line Rate = 27%, Branch Rate = 100%, Complexity = 11, ❌
Summary: Line Rate = 83% (1212 / 1460), Branch Rate = 69% (262 / 378), Complexity = 682, ✔
Minimum allowed line rate is 50%
```

#### Markdown Example
![image](https://user-images.githubusercontent.com/27953302/117726304-4ac1c100-b1de-11eb-8d9a-6286ba1f5523.png)

## Usage

```yaml
name: Code Coverage Summary Report
uses: irongut/CodeCoverageSummary@v1.0.5
with:
  filename: coverage/coverage.cobertura.xml
```

### .Net 5 Workflow Example

```yaml
name: .Net 5 CI Build

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
        dotnet-version: 5.0.x

    - name: Restore Dependencies
      run: dotnet restore src/Example.sln

    - name: Build
      run: dotnet build src/Example.sln --configuration Release --no-restore

    - name: Test
      run: dotnet test src/Example.sln --configuration Release --no-build --verbosity normal --collect:"XPlat Code Coverage" --results-directory ./coverage

    - name: Copy Coverage To Predictable Location
      run: cp coverage/**/coverage.cobertura.xml coverage/coverage.cobertura.xml

    - name: Code Coverage Summary Report
      uses: irongut/CodeCoverageSummary@v1.0.5
      with:
        filename: coverage/coverage.cobertura.xml
        badge: true
        format: 'markdown'
        output: 'both'

    - name: Add Coverage PR Comment
      uses: marocchino/sticky-pull-request-comment@v2
      if: github.event_name == 'pull_request'
      with:
        recreate: true
        path: code-coverage-results.md
```

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
