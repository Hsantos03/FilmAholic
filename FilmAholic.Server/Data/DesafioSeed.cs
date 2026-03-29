using System;
using System.Collections.Generic;
using FilmAholic.Server.Models;

namespace FilmAholic.Server.Data
{
    public static class DesafioSeed
    {
        private const int RECOMPENSA_XP = 25;

        public static List<Desafio> Desafios = new()
        {
            // Daily quiz challenges           
            new Desafio {
                Id = 1, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "Antes de se chamarem 'Cinemas NOS', qual era o nome desta que foi a maior rede de cinemas em Portugal?",
                OpcaoA = "Cinemas ZON",
                OpcaoB = "Cinemas Lusomundo",
                OpcaoC = "UCI Cinemas",
                RespostaCorreta = "B"
            },
            new Desafio {
                Id = 2, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "Qual atriz portuguesa interpretou a personagem 'Ratcatcher 2' no filme de Hollywood 'O Esquadrão Suicida'?",
                OpcaoA = "Daniela Melchior",
                OpcaoB = "Alba Baptista",
                OpcaoC = "Vitória Guerra",
                RespostaCorreta = "A"
            },
            new Desafio {
                Id = 3, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "Qual é a cor do famoso comprimido que Neo escolhe tomar no filme 'Matrix'?",
                OpcaoA = "Azul",
                OpcaoB = "Verde",
                OpcaoC = "Vermelho",
                RespostaCorreta = "C"
            },
            new Desafio {
                Id = 4, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "'Ó Evaristo, tens cá disto?' é uma frase icónica de qual clássico do cinema português?",
                OpcaoA = "O Leão da Estrela",
                OpcaoB = "A Canção de Lisboa",
                OpcaoC = "O Pátio das Cantigas",
                RespostaCorreta = "C"
            },
            new Desafio {
                Id = 5, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "Joaquim de Almeida, um dos mais conhecidos atores portugueses, foi o vilão Hernan Reyes em que saga internacional?",
                OpcaoA = "Velocidade Furiosa (Fast & Furious)",
                OpcaoB = "Missão Impossível",
                OpcaoC = "James Bond",
                RespostaCorreta = "A"
            },
            new Desafio {
                Id = 6, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "Quem interpretou o papel de Jack Dawson no épico 'Titanic' (1997)?",
                OpcaoA = "Brad Pitt",
                OpcaoB = "Leonardo DiCaprio",
                OpcaoC = "Tom Cruise",
                RespostaCorreta = "B"
            },
            new Desafio {
                Id = 7, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "Qual é o título da curta-metragem de animação portuguesa que fez história ao ser nomeada para os Óscares em 2023?",
                OpcaoA = "Ice Merchants",
                OpcaoB = "A Suspeita",
                OpcaoC = "O Homem do Lixo",
                RespostaCorreta = "A"
            },
            new Desafio {
                Id = 8, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "Na saga 'Star Wars', que icónico vilão diz a frase 'No, I am your father' (Não, eu sou o teu pai)?",
                OpcaoA = "Obi-Wan Kenobi",
                OpcaoB = "Darth Vader",
                OpcaoC = "Imperador Palpatine",
                RespostaCorreta = "B"
            },
            new Desafio {
                Id = 9, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "Onde foi inaugurada a primeira sala de cinema IMAX comercial em Portugal, abrindo as portas à tecnologia 3D gigante?",
                OpcaoA = "Centro Colombo (Lisboa)",
                OpcaoB = "NorteShopping (Matosinhos)",
                OpcaoC = "Almada Fórum (Almada)",
                RespostaCorreta = "A"
            },
            new Desafio {
                Id = 10, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "Que atriz portuguesa protagonizou a série de sucesso mundial 'Warrior Nun' da Netflix?",
                OpcaoA = "Daniela Ruah",
                OpcaoB = "Alba Baptista",
                OpcaoC = "Joana Ribeiro",
                RespostaCorreta = "B"
            },            
            new Desafio {
                Id = 11, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "Que ator dá vida ao brilhante bilionário Tony Stark (Iron Man) no universo Marvel?",
                OpcaoA = "Chris Evans",
                OpcaoB = "Robert Downey Jr.",
                OpcaoC = "Chris Hemsworth",
                RespostaCorreta = "B"
            },
            new Desafio {
                Id = 12, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "Em qual destas séries internacionais de sucesso da Netflix participou o ator português Nuno Lopes?",
                OpcaoA = "Stranger Things",
                OpcaoB = "White Lines",
                OpcaoC = "La Casa de Papel",
                RespostaCorreta = "B"
            },
            new Desafio {
                Id = 13, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "Como se chama a cicatriz característica na testa de Harry Potter?",
                OpcaoA = "Uma estrela",
                OpcaoB = "Um relâmpago",
                OpcaoC = "Uma meia-lua",
                RespostaCorreta = "B"
            },
            new Desafio {
                Id = 14, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "Tom Hanks ficou conhecido mundialmente por dar a voz original a que famoso boneco da Disney/Pixar?",
                OpcaoA = "Buzz Lightyear",
                OpcaoB = "Xerife Woody",
                OpcaoC = "Senhor Cabeça de Batata",
                RespostaCorreta = "B"
            },
            new Desafio {
                Id = 15, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "Em 2017, nos Óscares, o prémio de Melhor Filme foi dado por engano ao 'La La Land'. Qual foi o filme que realmente venceu?",
                OpcaoA = "Moonlight",
                OpcaoB = "Green Book",
                OpcaoC = "A Forma da Água",
                RespostaCorreta = "A"
            },
            new Desafio {
                Id = 16, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "Que ator interpretou de forma brilhante o Joker no filme 'O Cavaleiro das Trevas' (2008), ganhando um Óscar póstumo?",
                OpcaoA = "Joaquin Phoenix",
                OpcaoB = "Jack Nicholson",
                OpcaoC = "Heath Ledger",
                RespostaCorreta = "C"
            },
            new Desafio {
                Id = 17, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "Qual foi o primeiro grande estúdio de animação fundado em Portugal focado em stop-motion e animação 2D/3D?",
                OpcaoA = "Animanostra",
                OpcaoB = "Aardman Portugal",
                OpcaoC = "Pixar PT",
                RespostaCorreta = "A"
            },
            new Desafio {
                Id = 18, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "O ator português Pepê Rapazote ficou famoso internacionalmente por interpretar um barão da droga em que série?",
                OpcaoA = "Narcos",
                OpcaoB = "Breaking Bad",
                OpcaoC = "Ozark",
                RespostaCorreta = "A"
            },
            new Desafio {
                Id = 19, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "Em que ano foi lançado o primeiro filme da saga Star Wars?",
                OpcaoA = "1977",
                OpcaoB = "1980",
                OpcaoC = "1983",
                RespostaCorreta = "A"
            },
            new Desafio {
                Id = 20, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "Qual diretor foi responsável pelos filmes Inception e Interstellar?",
                OpcaoA = "Steven Spielberg",
                OpcaoB = "Martin Scorsese",
                OpcaoC = "Christopher Nolan",
                RespostaCorreta = "C"
            },
            new Desafio {
                Id = 21, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "No clássico português 'O Leão da Estrela' (1947), a personagem Anastácio é um adepto fanático de que clube?",
                OpcaoA = "Sporting Clube de Portugal",
                OpcaoB = "Sport Lisboa e Benfica",
                OpcaoC = "Futebol Clube do Porto",
                RespostaCorreta = "A"
            },
            new Desafio {
                Id = 22, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "No universo Marvel, o escudo do Capitão América é feito de que material indestrutível?",
                OpcaoA = "Adamantium",
                OpcaoB = "Titanium",
                OpcaoC = "Vibranium",
                RespostaCorreta = "C"
            },
            new Desafio {
                Id = 23, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "Qual é o nome do aterrorizante palhaço criado por Stephen King que protagoniza o filme 'It'?",
                OpcaoA = "Pennywise",
                OpcaoB = "Krusty",
                OpcaoC = "Joker",
                RespostaCorreta = "A"
            },
            new Desafio {
                Id = 24, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "A atriz portuguesa Maria de Medeiros entrou num dos maiores filmes de culto de Quentin Tarantino. Qual foi?",
                OpcaoA = "Kill Bill",
                OpcaoB = "Pulp Fiction",
                OpcaoC = "Sacanas Sem Lei (Inglourious Basterds)",
                RespostaCorreta = "B"
            },
            new Desafio {
                Id = 25, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "O ator português Diogo Morgado ficou conhecido mundialmente (com a alcunha de 'Hot Jesus') ao protagonizar o filme...",
                OpcaoA = "A Paixão de Cristo",
                OpcaoB = "O Filho de Deus (Son of God)",
                OpcaoC = "Noé",
                RespostaCorreta = "B"
            },
            new Desafio {
                Id = 26, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "Na famosa saga de magia, qual é o nome da escola frequentada por Harry Potter?",
                OpcaoA = "Hogwarts",
                OpcaoB = "Beauxbatons",
                OpcaoC = "Durmstrang",
                RespostaCorreta = "A"
            },
            new Desafio {
                Id = 27, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "Que famoso realizador de Hollywood nos trouxe clássicos como 'E.T. O Extraterrestre', 'Tubarão' e 'Parque Jurássico'?",
                OpcaoA = "James Cameron",
                OpcaoB = "Steven Spielberg",
                OpcaoC = "George Lucas",
                RespostaCorreta = "B"
            },
            new Desafio {
                Id = 28, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "Qual destas sagas épicas conseguiu ganhar o Óscar de Melhor Filme com o seu terceiro capítulo, 'O Regresso do Rei'?",
                OpcaoA = "Star Wars",
                OpcaoB = "O Senhor dos Anéis",
                OpcaoC = "O Padrinho",
                RespostaCorreta = "B"
            },
            new Desafio {
                Id = 29, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "O filme biográfico português de 2019, 'Variações', foca-se na vida de que icónico cantor?",
                OpcaoA = "Zeca Afonso",
                OpcaoB = "António Variações",
                OpcaoC = "Carlos do Carmo",
                RespostaCorreta = "B"
            },
            new Desafio {
                Id = 30, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "Qual é a verdadeira identidade secreta do super-herói Batman?",
                OpcaoA = "Clark Kent",
                OpcaoB = "Bruce Wayne",
                OpcaoC = "Peter Parker",
                RespostaCorreta = "B"
            },
            new Desafio {
                Id = 31, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "No filme 'O Rei Leão' (1994) da Disney, qual é o nome do tio malvado de Simba?",
                OpcaoA = "Mufasa",
                OpcaoB = "Jafar",
                OpcaoC = "Scar",
                RespostaCorreta = "C"
            },
            new Desafio {
                Id = 32, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "A frase imortal 'I'll be back' (Eu voltarei) foi popularizada por Arnold Schwarzenegger em que filme de ação?",
                OpcaoA = "O Exterminador Implacável (Terminator)",
                OpcaoB = "Predador",
                OpcaoC = "Assalto ao Arranha-Céus (Die Hard)",
                RespostaCorreta = "A"
            },
            new Desafio {
                Id = 33, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "Que ator imortalizou a personagem principal do franchise 'Piratas das Caraíbas', o Capitão Jack Sparrow?",
                OpcaoA = "Orlando Bloom",
                OpcaoB = "Brad Pitt",
                OpcaoC = "Johnny Depp",
                RespostaCorreta = "C"
            },
            new Desafio {
                Id = 34, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "Qual foi o primeiro filme da história a ultrapassar os 2 mil milhões de dólares de bilheteira?",
                OpcaoA = "Titanic",
                OpcaoB = "Avatar",
                OpcaoC = "Avengers: Endgame",
                RespostaCorreta = "A"
            },
            new Desafio {
                Id = 35, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "Em 'O Silêncio dos Inocentes', qual é o nome do famoso serial killer canibal interpretado por Anthony Hopkins?",
                OpcaoA = "Norman Bates",
                OpcaoB = "Hannibal Lecter",
                OpcaoC = "Patrick Bateman",
                RespostaCorreta = "B"
            },
            new Desafio {
                Id = 36, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "No filme 'Forrest Gump', qual é a famosa frase que a mãe de Forrest lhe diz sobre a vida?",
                OpcaoA = "A vida é uma aventura",
                OpcaoB = "A vida é como uma caixa de chocolates",
                OpcaoC = "A vida é o que tu fazes dela",
                RespostaCorreta = "B"
            },
            new Desafio {
                Id = 37, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "Qual realizador dirigiu 'Pulp Fiction', 'Django Libertado' e 'Once Upon a Time in Hollywood'?",
                OpcaoA = "Quentin Tarantino",
                OpcaoB = "David Fincher",
                OpcaoC = "Ridley Scott",
                RespostaCorreta = "A"
            },
            new Desafio {
                Id = 38, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "Em que país se passa a ação do filme de animação 'Coco' da Pixar?",
                OpcaoA = "Brasil",
                OpcaoB = "Espanha",
                OpcaoC = "México",
                RespostaCorreta = "C"
            },
            new Desafio {
                Id = 39, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "Qual foi o primeiro filme de animação da história do cinema, lançado pela Disney em 1937?",
                OpcaoA = "Pinóquio",
                OpcaoB = "Bambi",
                OpcaoC = "Branca de Neve e os Sete Anões",
                RespostaCorreta = "C"
            },
            new Desafio {
                Id = 40, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "No filme 'O Club da Luta' (Fight Club), qual é a primeira regra do clube?",
                OpcaoA = "Nunca fales do clube",
                OpcaoB = "Nunca pares de lutar",
                OpcaoC = "Só dois homens por luta",
                RespostaCorreta = "A"
            },
            new Desafio {
                Id = 41, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "Em 'Jurassic Park', qual é o nome do sistema de segurança informático que a jovem Lex tenta reativar?",
                OpcaoA = "UNIX",
                OpcaoB = "NEXUS",
                OpcaoC = "GRID",
                RespostaCorreta = "A"
            },
            new Desafio {
                Id = 42, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "Qual ator interpretou o vilão Thanos nos filmes da Marvel?",
                OpcaoA = "Idris Elba",
                OpcaoB = "Josh Brolin",
                OpcaoC = "Andy Serkis",
                RespostaCorreta = "B"
            },
            new Desafio {
                Id = 43, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "O filme 'Parasita' (Parasite) foi o primeiro filme não falado em inglês a vencer o Óscar de Melhor Filme. De que país é originário?",
                OpcaoA = "Japão",
                OpcaoB = "China",
                OpcaoC = "Coreia do Sul",
                RespostaCorreta = "C"
            },
            new Desafio {
                Id = 44, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "No filme 'A Origem' (Inception), como se chama o objeto que Cobb usa para saber se está num sonho?",
                OpcaoA = "Um pião",
                OpcaoB = "Um cubo",
                OpcaoC = "Uma moeda",
                RespostaCorreta = "A"
            },
            new Desafio {
                Id = 45, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "Qual atriz interpretou Katniss Everdeen na saga 'Jogos da Fome' (The Hunger Games)?",
                OpcaoA = "Shailene Woodley",
                OpcaoB = "Jennifer Lawrence",
                OpcaoC = "Emma Watson",
                RespostaCorreta = "B"
            },
            new Desafio {
                Id = 46, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "Qual é o nome do boneco de neve mágico no filme de animação 'Frozen'?",
                OpcaoA = "Sven",
                OpcaoB = "Olaf",
                OpcaoC = "Kristoff",
                RespostaCorreta = "B"
            },
            new Desafio {
                Id = 47, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "No filme 'O Padrinho' (The Godfather), qual é o apelido da família mafiosa protagonista?",
                OpcaoA = "Soprano",
                OpcaoB = "Corleone",
                OpcaoC = "Gambino",
                RespostaCorreta = "B"
            },
            new Desafio {
                Id = 48, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "Em 'Interstellar', qual é o nome da nave espacial tripulada pelos protagonistas?",
                OpcaoA = "Endurance",
                OpcaoB = "Discovery",
                OpcaoC = "Ranger",
                RespostaCorreta = "A"
            },
            new Desafio {
                Id = 49, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "Qual ator ganhou o Óscar de Melhor Ator por 'O Gladiador' (Gladiator) no ano 2001?",
                OpcaoA = "Russell Crowe",
                OpcaoB = "Mel Gibson",
                OpcaoC = "Tom Hanks",
                RespostaCorreta = "A"
            },
            new Desafio {
                Id = 50, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "No filme 'Alien' (1979), qual é o nome da inteligência artificial da nave Nostromo?",
                OpcaoA = "HAL 9000",
                OpcaoB = "MOTHER",
                OpcaoC = "ARIA",
                RespostaCorreta = "B"
            },
            new Desafio {
                Id = 51, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "No filme 'Goodfellas' (Os Bons Tipos, 1990) de Scorsese, com que famosa frase começa a narração de Henry Hill?",
                OpcaoA = "Sempre quis ser um gangster",
                OpcaoB = "Nova Iorque era a minha cidade",
                OpcaoC = "A máfia não existe",
                RespostaCorreta = "A"
            },
            new Desafio {
                Id = 52, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "Em 'O Senhor dos Anéis: A Sociedade do Anel', qual é o nome da mina subterrânea dos anões que o grupo tenta atravessar?",
                OpcaoA = "Mordor",
                OpcaoB = "Minas Tirith",
                OpcaoC = "Minas Morgul",
                RespostaCorreta = "B"
            },
            new Desafio {
                Id = 53, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "No filme 'Whiplash' (2014), que instrumento toca o jovem Andrew Neiman, perseguido pelo professor Fletcher?",
                OpcaoA = "Violino",
                OpcaoB = "Bateria",
                OpcaoC = "Trompete",
                RespostaCorreta = "B"
            },
            new Desafio {
                Id = 54, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "Em 'Ratatouille' (2007), em que famosa cidade europeia se passa a história do rato que sonha ser chef?",
                OpcaoA = "Roma",
                OpcaoB = "Paris",
                OpcaoC = "Barcelona",
                RespostaCorreta = "B"
            },
            new Desafio {
                Id = 55, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "Em 'Oppenheimer' (2023), que ator interpreta o cientista J. Robert Oppenheimer?",
                OpcaoA = "Matt Damon",
                OpcaoB = "Cillian Murphy",
                OpcaoC = "Christian Bale",
                RespostaCorreta = "B"
            },
            new Desafio {
                Id = 56, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "No filme 'Barbie' (2023), que ator interpreta o Ken principal ao lado de Margot Robbie?",
                OpcaoA = "Timothée Chalamet",
                OpcaoB = "Zac Efron",
                OpcaoC = "Ryan Gosling",
                RespostaCorreta = "C"
            },
            new Desafio {
                Id = 57, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "Em 'Dune: Parte 2' (2024), que atriz interpreta a personagem Chani, amor de Paul Atreides?",
                OpcaoA = "Zendaya",
                OpcaoB = "Florence Pugh",
                OpcaoC = "Anya Taylor-Joy",
                RespostaCorreta = "A"
            },
            new Desafio {
                Id = 58, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "Qual foi o filme vencedor do Óscar de Melhor Filme na cerimónia de 2024?",
                OpcaoA = "Barbie",
                OpcaoB = "Poor Things",
                OpcaoC = "Oppenheimer",
                RespostaCorreta = "C"
            },
            new Desafio {
                Id = 59, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "Em 'Everything Everywhere All at Once' (2022), que atriz ganhou o Óscar de Melhor Atriz pelo papel principal?",
                OpcaoA = "Cate Blanchett",
                OpcaoB = "Michelle Yeoh",
                OpcaoC = "Ana de Armas",
                RespostaCorreta = "B"
            },
            new Desafio {
                Id = 60, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "No filme 'Top Gun: Maverick' (2022), que ator regressa ao papel de Pete 'Maverick' Mitchell?",
                OpcaoA = "Tom Cruise",
                OpcaoB = "Brad Pitt",
                OpcaoC = "Keanu Reeves",
                RespostaCorreta = "A"
            },
            new Desafio {
                Id = 61, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "Em 'The Last of Us' (série HBO, 2023), que ator interpreta Joel Miller?",
                OpcaoA = "Pedro Pascal",
                OpcaoB = "Oscar Isaac",
                OpcaoC = "Joel Edgerton",
                RespostaCorreta = "A"
            },
            new Desafio {
                Id = 62, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "No filme 'Spider-Man: No Way Home' (2021), quantas versões diferentes do Spider-Man aparecem juntas no ecrã?",
                OpcaoA = "2",
                OpcaoB = "3",
                OpcaoC = "4",
                RespostaCorreta = "B"
            },
            new Desafio {
                Id = 63, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "No filme 'Poor Things' (2023) de Yorgos Lanthimos, que atriz interpreta Bella Baxter, ganhando o Óscar de Melhor Atriz?",
                OpcaoA = "Emma Stone",
                OpcaoB = "Olivia Colman",
                OpcaoC = "Rachel Weisz",
                RespostaCorreta = "A"
            },
            new Desafio {
                Id = 64, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "Em 'Avengers: Endgame' (2019), que personagem usa as Joias do Infinito para eliminar Thanos e os seus exércitos?",
                OpcaoA = "Thor",
                OpcaoB = "Capitão América",
                OpcaoC = "Tony Stark (Iron Man)",
                RespostaCorreta = "C"
            },
            new Desafio {
                Id = 65, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "Na série 'Wednesday' (Netflix, 2022), que atriz interpreta a Wednesday Addams?",
                OpcaoA = "Millie Bobby Brown",
                OpcaoB = "Jenna Ortega",
                OpcaoC = "Sadie Sink",
                RespostaCorreta = "B"
            },
            new Desafio {
                Id = 66, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "Em 'Wonka' (2023), que ator interpreta o jovem Willy Wonka antes de ter a sua famosa fábrica de chocolate?",
                OpcaoA = "Paul Mescal",
                OpcaoB = "Timothée Chalamet",
                OpcaoC = "Tom Holland",
                RespostaCorreta = "B"
            },
            new Desafio {
                Id = 67, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "Qual foi o primeiro filme live-action baseado num videojogo a ser considerado um sucesso crítico e comercial, lançado em 2023?",
                OpcaoA = "Gran Turismo",
                OpcaoB = "Five Nights at Freddy's",
                OpcaoC = "The Super Mario Bros. Movie",
                RespostaCorreta = "C"
            },
            new Desafio {
                Id = 68, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "Na série 'The Bear' (Disney+), que tipo de restaurante tenta reconverter o chef Carmy em Chicago?",
                OpcaoA = "Uma pizzaria italiana",
                OpcaoB = "Uma sandwicheria de família",
                OpcaoC = "Um restaurante de sushi",
                RespostaCorreta = "B"
            },
            new Desafio {
                Id = 69, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "Em 'Moana 2' (2024), para onde parte Moana numa nova aventura épica pelos oceanos?",
                OpcaoA = "Um arquipélago perdido há gerações",
                OpcaoB = "O reino do deus Maui",
                OpcaoC = "Uma ilha de monstros marinhos",
                RespostaCorreta = "A"
            },
            new Desafio {
                Id = 70, DataInicio = DateTime.Today, DataFim = DateTime.Today,
                Descricao = "Quiz diário sobre cinema", Ativo = true, Genero = "Quiz", QuantidadeNecessaria = 1, Xp = RECOMPENSA_XP,
                Pergunta = "No filme 'Wicked' (2024), que atriz interpreta Elphaba, a Bruxa Má do Oeste antes de se tornar vilã?",
                OpcaoA = "Ariana Grande",
                OpcaoB = "Cynthia Erivo",
                OpcaoC = "Halle Bailey",
                RespostaCorreta = "B"
            },
        };
    }
}
