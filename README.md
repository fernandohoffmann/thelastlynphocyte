# 🧬 The Last Lymphocyte

> *Você é uma das últimas células de defesa de um paciente à beira da falência imunológica. Atravesse o corpo, destrua os invasores e restabeleça a imunidade do hospedeiro.*

**The Last Lymphocyte** é um roguelike de ação 2D top-down no estilo *bullet hell / survivor-like*, ambientado inteiramente **dentro do corpo humano**. O jogador controla um **linfócito** e enfrenta hordas de bactérias, vírus e doenças reais. Cada arma, cada inimigo e cada melhoria é baseado em um conceito real de imunologia, e o jogo tem um **Microscópio** (enciclopédia) que ensina esses conceitos conforme você os descobre.

Projeto desenvolvido para o **PAC III — Projeto de Aprendizagem Colaborativa Extensionista III**, curso de Engenharia de Software (turma B) do **Centro Universitário Católica de Santa Catarina**, Joinville/SC.

| | |
|---|---|
| **Gênero** | Roguelike Action RPG / Bullet Hell (survivor-like) — *Serious Game* (Saúde e Medicina) |
| **Plataforma** | PC (Windows) |
| **Jogadores** | 1 (single-player) |
| **Engine** | Godot 4.6 (.NET) — C# |
| **Público-alvo** | A partir de 14 anos: fãs de *Vampire Survivors* e *Hades*, e interessados em biologia e saúde |

---

## 📖 Sumário

1. [Como jogar](#-como-jogar)
2. [Controles](#-controles)
3. [Fases e missões](#-fases-e-missões)
4. [Armas](#-armas)
5. [Passivas](#-passivas)
6. [Inimigos](#-inimigos)
7. [Progressão](#-progressão)
8. [Microscópio (Codex)](#-microscópio-codex)
9. [Direção de arte e áudio](#-direção-de-arte-e-áudio)
10. [GDD x jogo: o que entrou e o que não entrou](#-gdd-x-jogo-o-que-entrou-e-o-que-não-entrou)
11. [Como rodar o projeto](#-como-rodar-o-projeto)
12. [Estrutura do repositório](#-estrutura-do-repositório)
13. [Equipe](#-equipe)

---

## 🎮 Como jogar

1. No **menu principal**, escolha **Começar Jogo** para abrir o **Loadout**.
2. No Loadout, selecione a **fase** e monte seu equipamento: até **3 armas** e **5 passivas**. Só aparece o que você já desbloqueou.
3. Clique em **Iniciar Partida**. Uma introdução em vídeo toca antes de entrar no corpo.
4. Dentro da partida, **mova-se e desvie**: as armas atacam sozinhas.
5. Derrote inimigos para coletar **XP**. Ao encher a barra você **sobe de nível** e escolhe **1 entre 3 melhorias**.
6. Cumpra as **missões** da fase antes de morrer. Cada partida tem um **limite de 20 minutos** e a dificuldade cresce com o tempo.
7. Os inimigos também deixam **DNA**, moeda permanente que você gasta na **Árvore de Habilidades** para desbloquear novas armas e passivas para as próximas partidas.

Na **primeira partida** e na **primeira vez que abrir o Loadout**, o jogo mostra um tutorial. Na partida, o tempo entra em câmera lenta enquanto aparece a tecla da vez (W, D, S, A). Quando você aperta, o tempo volta ao normal e vem a próxima. O tutorial aparece uma única vez (**Backspace** pula) e some do jogo para sempre.

## 🕹️ Controles

| Ação | Tecla |
|---|---|
| Mover | `W` `A` `S` `D` ou setas |
| Mirar (Anticorpos) | Mouse |
| Pausar / continuar | `Esc` |
| Mapa grande | Segure `Tab` |
| Escolher melhoria / equipar item | Clique do mouse |
| Pular tutorial | `Backspace` |

## 🗺️ Fases e missões

O jogo tem **duas fases**, e cada uma muda o cenário, o mapa, a paleta de cores dos menus e as regras. A fase 2 é liberada ao concluir a fase 1.

### Fase 1 — Sistema Circulatório (veias)
Túneis de veias com paredes sólidas, glóbulos vermelhos à deriva e capilares pulsando. Inimigos e colônias contornam as paredes. Há um minimapa que vai sendo revelado conforme você explora.

| # | Missão | Objetivo |
|---|---|---|
| 1 | Resistir ao primeiro ataque | Sobreviver 300 s |
| 2 | Conter a infecção na corrente sanguínea | Matar 30 bactérias |
| 3 | Localizar o foco da infecção | Destruir 3 colônias bacterianas |
| 4 | Purificar a corrente sanguínea | Matar 50 inimigos em 180 s |
| 5 | Eliminar a Doença de Chagas | **Chefe:** derrotar o *Trypanosoma cruzi* |

### Fase 2 — Pulmões
Cenário de pulmão com a **respiração** empurrando suavemente o jogador e alvéolos pulsando, em tons de roxo. As bactérias ficam roxas e **20% se dividem em duas** ao encostar no jogador. **Não há chefe** nesta fase.

| # | Missão | Objetivo |
|---|---|---|
| 1 | Resistir à invasão dos brônquios | Sobreviver 240 s |
| 2 | Eliminar as bactérias respiratórias | Matar 40 bactérias |
| 3 | Neutralizar os vírus do pulmão | Matar 20 vírus |
| 4 | Destruir os focos de pneumonia | Destruir 4 colônias |
| 5 | Limpar os alvéolos | Matar 60 inimigos em 150 s |

## ⚔️ Armas

Todas as armas atacam **automaticamente** e evoluem até o **nível 5** durante a partida.

| Arma | Base biológica | Como funciona no jogo |
|---|---|---|
| **Anticorpos** *(inicial)* | Proteínas em Y que reconhecem e neutralizam patógenos | Projéteis em direção ao cursor |
| **Anticorpos Guiados** | Anticorpos monoclonais, específicos para cada antígeno | Projéteis que perseguem inimigos |
| **Proteína de Complemento** | Cascata de proteínas que amplifica a resposta imune | Raios que saltam de inimigo em inimigo |
| **Tentáculo Fagocitário** | Pseudópodos dos fagócitos engolindo patógenos | Golpeia inimigos próximos, como um chicote |
| **Tempestade de Citocinas** | Resposta inflamatória exagerada | Nuvem de veneno periódica que causa dano contínuo |
| **Macrófago** | Fagocitose | Aliado lento que engole inimigos. Cresce a cada um e, ao engolir 3, **explode** em área |

## 🧪 Passivas

Equipe até 5 no Loadout. Cada uma sobe até o nível 5.

| Passiva | Efeito |
|---|---|
| Sistema Imune Reforçado | +5 HP máximo por nível |
| Metabolismo Acelerado | −15% de cooldown das armas |
| Produção de Anticorpos | +15% de velocidade de ataque |
| Circulação Acelerada | +12% de velocidade de movimento |
| Síntese de DNA | +0,5 DNA por inimigo |
| Antibiótico | +25% de dano contra bactérias |
| Antiviral | +25% de dano contra vírus |
| Hemoglobina Reativa | Inimigos mortos explodem em área |
| Fator de Crescimento | +25% de XP ganho |
| Quimiotaxia | Itens do chão são atraídos para você |
| Hemoglobina Aprimorada | +25% de cura dos corações |
| Plaquetas | +0,5 s de imunidade após levar dano |
| Adrenalina | Mais dano e velocidade conforme o HP perdido |

## 🦠 Inimigos

| Inimigo | Comportamento |
|---|---|
| **Bactéria** | Rápida e frágil, persegue o jogador. Na fase 2, é roxa e pode se dividir em duas |
| **Vírus** | Mais resistente e causa mais dano, persegue o jogador |
| **Colônia bacteriana** | Objetivo de missão. Parada, aguenta bastante dano e **gera bactérias** continuamente. Destruí-la deixa os spawns mais lentos |
| **Doença de Chagas** *(chefe da fase 1)* | Enorme, 2600 HP. Persegue o jogador, morde, **cospe projéteis** e causa **febre** (dano contínuo) |

Os inimigos desviam das paredes das veias com pathfinding. A dificuldade e a quantidade de inimigos aumentam com o tempo da partida.

**Drops:** XP em quatro tamanhos (pequeno, médio, grande e raro), DNA ao morrer e, com 5% de chance, um **coração** que cura.

## 📈 Progressão

- **Dentro da partida:** sobe de nível com XP e escolhe 1 de 3 melhorias (armas e passivas do seu loadout). Vários level-ups seguidos aparecem um de cada vez.
- **Entre partidas:** o **DNA** coletado é gasto na **Árvore de Habilidades**, com 6 armas e 13 passivas em ordem de custo crescente.
- **Fases:** concluir todas as missões de uma fase libera a seguinte.
- O progresso (DNA, itens, descobertas do Codex, fases liberadas, configurações e tutoriais vistos) é **salvo automaticamente**.

## 🔬 Microscópio (Codex)

O **Microscópio** é a parte educativa do jogo: uma enciclopédia com **24 entradas** que vão sendo **descobertas jogando** (ao usar uma arma, enfrentar um inimigo, entrar numa região, etc.). Um aviso aparece na tela a cada descoberta. A navegação é feita por um corpo humano desenhado, em que cada órgão é uma seção.

As entradas ficam em seis categorias: **Estruturas**, **Células**, **Defesa**, **Ameaças**, **Processos** e **Doenças**. Alguns exemplos: fagocitose, inflamação, coagulação, circulação sanguínea, *Trypanosoma cruzi* e doença de Chagas.

O aprendizado acontece de forma **indireta**, pelas próprias mecânicas. O jogo não é um quiz.

## 🎨 Direção de arte e áudio

- **Visual:** estilo 2D biológico, tons de sangue e vermelho profundo na fase 1 e roxos nos pulmões. Cenários, paredes e células usam shaders por código (distorção das células, fumaça das citocinas, pulso do coração). Há vinheta que escurece conforme a vida diminui e iluminação pulsante.
- **Interface:** menus no mesmo estilo (molduras arredondadas), com a paleta mudando conforme a fase selecionada.
- **Áudio:** música de rock durante as partidas, **batimento cardíaco** de fundo que acelera com o tempo de jogo e efeitos sonoros de dano, coleta, level-up e interface.
- **Opções:** volume de música e de efeitos, tela cheia e resolução (1280×720 até 2560×1440).

## 📋 GDD x jogo: o que entrou e o que não entrou

O **Game Design Document** foi escrito na pré-produção e descreve um escopo maior do que o que foi possível entregar. Esta tabela mostra o que de fato está no jogo.

| Item do GDD | Situação | Observação |
|---|---|---|
| Roguelike de ação top-down, ataque automático, movimento WASD | ✅ Implementado | Núcleo do jogo |
| Evolução em tempo real (nível → escolher 1 de 3) | ✅ Implementado | Com fila para vários level-ups |
| Biologia como mecânica | ✅ Implementado | Armas e passivas baseadas em imunologia real |
| Inimigos: vírus, bactérias | ✅ Implementado | Com colônias e divisão de bactérias (fase 2) |
| Chefe de região (doença real) | ✅ Parcial | Só a **Doença de Chagas** (fase 1). A fase 2 não tem chefe |
| Regiões do corpo | ✅ Parcial | **Corrente sanguínea (veias)** e **pulmões**. Estômago e cérebro não entraram |
| Correntes de ambiente empurrando a célula (ex.: ar no pulmão) | ✅ Implementado | Respiração do pulmão na fase 2 |
| Desbloqueio de novas regiões ao vencer | ✅ Implementado | Concluir a fase 1 libera a fase 2 |
| Coleta de DNA e XP | ✅ Implementado | Proteínas e energia celular viraram só XP e DNA |
| Meta-progressão entre partidas | ✅ Adaptado | Em vez da Célula de Memória, o jogo usa a **Árvore de Habilidades** com DNA |
| Slots de upgrade passivo | ✅ Adaptado | Passivas equipadas no **Loadout** antes da partida |
| HUD (vida, mapa, avisos) | ✅ Implementado | Com minimapa, barra de XP, vida, DNA, tempo e missão atual |
| Menu principal, tela de evolução, inventário | ✅ Implementado | Redesenhados em estilo próprio |
| Sistema de missões | ✅ Implementado | 5 missões por fase. Sem o NPC Médico, aparecem direto na HUD |
| Efeitos visuais de infecção, pulso do coração | ✅ Implementado | Vinheta de vida, iluminação e batimento |
| Aspecto de Serious Game | ✅ Implementado | Microscópio com 24 entradas e fidelidade científica |
| Células jogáveis: Neutrófilo, Macrófago, Linfócito B/T, NK, Memória | ❌ Não entrou | O jogo tem **uma célula jogável: o Linfócito**. O Macrófago virou uma arma-aliada |
| Habilidades ativas por clique do mouse | ❌ Não entrou | Todas as armas são automáticas. O mouse só mira |
| Sistema de Imunidade Adquirida (buff contra o patógeno que te derrotou) | ❌ Não entrou | Substituído pela Árvore de Habilidades |
| NPC "O Médico" (ordens em tempo real) | ❌ Não entrou | Missões aparecem direto na HUD |
| Estômago (ácido), Cérebro (controles alterados) e seus chefes | ❌ Não entrou | Só as fases 1 e 2 |
| Chefes: Pneumonia, Superbactéria, Vírus mutante, Infecção cerebral | ❌ Não entrou | Só a Doença de Chagas |
| Áreas secretas (capilares ocultos com itens raros) | ❌ Não entrou | — |
| Inimigos que alteram o cenário (áreas contaminadas); fungos | ❌ Não entrou | — |
| Máquina de estados completa (patrulha/perseguição/ataque/mutação) | ⚠️ Simplificado | Inimigos perseguem com pathfinding. A divisão das bactérias e os ataques do chefe estão no jogo |
| Obstáculos que causam dano de contato | ⚠️ Simplificado | As paredes são sólidas e bloqueiam, mas não causam dano |
| Ciclo completo de animações (Idle/Walk/Run/Attack/Hit/Death/Evolve) | ⚠️ Parcial | Respiração e distorção das células, mordida do chefe, divisão das bactérias, explosões |
| Música por contexto (menu, exploração, combate, chefe, vitória, derrota) | ⚠️ Parcial | Música de rock nas partidas e batimento cardíaco. Sem trilhas separadas por situação |
| Paleta por região (azul/cinza no pulmão, amarelo no estômago, violeta no cérebro) | ⚠️ Parcial | Vermelho (veias) e roxo (pulmão) |

## 🚀 Como rodar o projeto

### Requisitos

- [Godot 4.6 **.NET**](https://godotengine.org/download) (a versão com suporte a C#)
- [.NET SDK](https://dotnet.microsoft.com/download) compatível com a sua versão do Godot
- [Git](https://git-scm.com/) e [Git LFS](https://git-lfs.com/) (o projeto usa LFS para imagens, áudio e vídeo)

### Passo a passo

```bash
git lfs install
git clone https://github.com/fernandohoffmann/thelastlynphocyte.git
cd thelastlynphocyte
```

1. Abra o **Godot 4.6 .NET** e clique em **Importar**.
2. Selecione o arquivo `project.godot`.
3. Aguarde a importação dos assets e pressione **F5** para jogar. A cena principal é o menu.

> Se aparecer o erro *".NET assemblies not found"*, abra o projeto pelo Godot (não direto de dentro de um `.zip`) e use **Build** (ícone de martelo) para compilar o C# antes de rodar.

## 🗂️ Estrutura do repositório

```
├── project.godot        # configuração do projeto Godot
├── Scenes/              # cenas (.tscn): menu, jogo, HUD, telas
├── Scripts/             # todo o código C#
│   ├── GameManager.cs         # estado da partida, tempo, dificuldade
│   ├── GameScene.cs           # monta a fase (mapa, HUD, missões, tutorial)
│   ├── GamePhase.cs           # dados de cada fase (cenário, cores, missões)
│   ├── MissionManager.cs      # missões por fase
│   ├── Player.cs              # o linfócito
│   ├── Enemy.cs / Bacteria.cs / Virus.cs / Colony.cs / ChagasBoss.cs
│   ├── Weapon*.cs             # as 6 armas
│   ├── UpgradeManager.cs      # armas, passivas e níveis
│   ├── VeinMap.cs             # paredes, pathfinding e mapa
│   ├── LungBackground.cs / LungBreath.cs   # fase dos pulmões
│   ├── Codex.cs / MicroscopeScreen.cs      # Microscópio
│   ├── MainMenu.cs / LoadoutScreen.cs / ArsenalScreen.cs / MenuStyle.cs
│   ├── TutorialOverlay.cs / LoadoutHint.cs # tutoriais
│   └── SaveData.cs            # salvamento do progresso
└── Assets/              # sprites, fundos, fontes, áudio e vídeo
```

## 👥 Equipe

- **Fernando Hoffman**
- **Henrique Gabriel de Oliveira**
- **Matheus Maciel**

Engenharia de Software — Turma B · Centro Universitário Católica de Santa Catarina · Joinville, 2026.
