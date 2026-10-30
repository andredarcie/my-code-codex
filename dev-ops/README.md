# DevOps

Docker, pipelines, versionamento, deploys e boas práticas de CI/CD. A cultura por trás é uma só: quem escreve o código também é responsável por ele chegar e se manter em produção.

- [Git](./git.md): commits semânticos, Git Flow, trunk-based, SemVer e o que esperam de um pull request.
- [Docker e Kubernetes](./docker.md): Dockerfile multi-stage para .NET, imagem enxuta e o mínimo de Kubernetes que um dev backend precisa saber.
- [CI/CD](./ci-cd.md): pipeline no GitHub Actions do build ao deploy, com testes e migrations.

## O que considero entregue

- Build e testes rodam automaticamente em todo pull request.
- A imagem que foi testada é a mesma que vai para produção, só muda a configuração.
- Deploy sem passo manual além de aprovar.
- Rollback em minutos, sem precisar de quem escreveu o código.
- Health check, logs e métricas no ar desde o primeiro deploy.
