# Optional: SonarQube Cloud (SonarCloud)

SonarQube Cloud is not enabled by default because it needs an account and a token. CI already covers
formatting, warnings as errors, tests, coverage thresholds, vulnerable packages, secrets and CodeQL;
SonarQube Cloud adds code smells, duplication and a quality gate on new code.

A ready-made workflow is in [`.github/workflows/sonar.yml.disabled`](../../.github/workflows/sonar.yml.disabled).
It uses [SonarScanner for .NET](https://docs.sonarsource.com/sonarqube-cloud/advanced-setup/ci-based-analysis/sonarscanner-for-dotnet/introduction/)
and reuses `scripts/coverage.sh`, which writes the Cobertura report that SonarQube Cloud reads through
`sonar.cs.cobertura.reportsPaths` ([coverage docs](https://docs.sonarsource.com/sonarqube-cloud/enriching/test-coverage/dotnet-test-coverage/)).

## Enable it

1. Sign in to <https://sonarcloud.io> with GitHub and import the repository. Note the **organization key**
   and the **project key**.
2. In the project, open **Administration → Analysis Method** and turn **off** Automatic Analysis
   (it conflicts with CI-based analysis).
3. Create a token: **My Account → Security → Generate Tokens**.
4. In GitHub, open **Settings → Secrets and variables → Actions**:
   - Secret `SONAR_TOKEN`: the token.
   - Variables `SONAR_ORGANIZATION` and `SONAR_PROJECT_KEY`: the keys from step 1.
5. Rename `.github/workflows/sonar.yml.disabled` to `sonar.yml`, check `SONARSCANNER_VERSION` against
   <https://www.nuget.org/packages/dotnet-sonarscanner>, then commit and push.
6. Optional: add the quality gate badge from the SonarQube Cloud project page to the README.

## Notes

- Pull requests from forks do not receive secrets, so the job skips them.
- No credential is stored in the repository: the token only lives in GitHub secrets.
- Plans and limits change; check the current [SonarQube Cloud pricing](https://www.sonarsource.com/plans-and-pricing/sonarcloud/)
  before relying on the free tier.
