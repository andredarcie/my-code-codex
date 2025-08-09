# Git

Commit semântico
```
feat: add [feature-name]
fix: resolve [issue-description]
docs: update [documentation-section]
style: adjust [style-change-description]
refactor: refactor [component-or-function-name]
perf: improve performance of [process-or-function-name]
test: add tests for [component-or-function-name]
chore: update [dependency-or-config-name]
```

Git Flow
```
main → código pronto para produção.
develop → código em desenvolvimento.
feature/ → novas funcionalidades, partem de develop e voltam para develop.
release/ → preparação para nova versão, parte de develop e vai para main e develop.
hotfix/ → correções urgentes em produção, partem de main e voltam para main e develop.
```

No Git Flow original do Vincent Driessen, a branch release é basicamente o que hoje muita gente chama de homologação / staging / pré-produção.

O código sai de feature → develop → release → main, e ao chegar no main é considerado pronto para produção.

Fluxo
```
main  <---- hotfix ---->  main
  ↑                       ↑
   \                     /
    release <---------- release
      ↑                 ↑
       develop <------ develop
         ↑
         feature
```