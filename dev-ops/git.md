# Git

## Commit semântico
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

## Git Flow
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

## Trunk-based development

O Git Flow ainda aparece muito em empresa com release agendada e homologação manual. Times com deploy contínuo costumam usar trunk-based: todo mundo integra na `main` pelo menos uma vez por dia, com branches que vivem horas, não semanas.

```
main ──●──●──●──●──●──●──  sempre implantável
        \  /  \  /
        feat   fix         branches curtas, um PR pequeno cada
```

- Funcionalidade incompleta entra desligada por **feature flag**, não numa branch de três semanas.
- Merge conflict pequeno e frequente no lugar de um conflito enorme na véspera da entrega.
- Exige pipeline confiável: se a `main` quebra, consertá-la é a prioridade do time.

Qual usar depende de como a empresa entrega. Em entrevista, saber explicar os dois e quando cada um faz sentido vale mais que defender um.

## Pull request

- **Pequeno.** Até umas 400 linhas, um assunto só. PR de 3.000 linhas recebe "LGTM" sem ninguém ter lido.
- **Descrição que responde:** o que muda, por quê, como testar, o que pode quebrar. Modelo em [Comunicação escrita](../soft-skills/comunicacao.md).
- **Histórico limpo:** `git rebase -i` para juntar os commits de "ajuste" antes de pedir revisão, ou squash no merge.
- **Revisado por alguém, testado pela pipeline.** Ver [Code review](../soft-skills/code-review.md).

## Comandos que salvam o dia

```bash
git switch -c feat/aprovacao-pedido          # nova branch
git commit --fixup <sha>                     # correção de um commit anterior
git rebase -i --autosquash main              # junta os fixups no lugar certo
git rebase main                              # atualiza a branch sem commit de merge
git cherry-pick <sha>                        # leva um commit para outra branch (hotfix)
git bisect start; git bisect bad; git bisect good v1.4.0   # acha o commit que quebrou
git reflog                                   # recupera o que parecia perdido
git push --force-with-lease                  # força com segurança na sua branch
```

`--force` em branch compartilhada apaga o trabalho dos outros. `--force-with-lease` recusa se alguém enviou algo que você não tem.

## Semantic Versioning (SemVer)

Formato: MAJOR.MINOR.PATCH  
MAJOR: mudanças incompatíveis com versões anteriores (breaking changes).  
MINOR: novas funcionalidades sem quebrar compatibilidade.  
PATCH: correções e ajustes internos, sem alterar a API.  
Tags Git: sempre marcar releases (git tag vX.Y.Z && git push origin vX.Y.Z).  

## O que perguntam em entrevista

- Diferença entre merge e rebase, e quando cada um é perigoso.
- Git Flow ou trunk-based: qual você usaria e por quê.
- Como desfazer um commit que já foi para a branch principal.
- Como você acharia o commit que introduziu um bug.
- O que muda na versão quando você remove um campo de uma API pública.
