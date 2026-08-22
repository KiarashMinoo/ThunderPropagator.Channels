using ThunderPropagator.Channels.Demo.Quiz.Game.Exceptions;

namespace ThunderPropagator.Channels.Demo.Quiz.Game
{
    /// <summary>
    /// The demo's built-in, server-side-only question set. <see cref="CreateDefault"/> supplies real
    /// trivia questions rather than Bogus-generated text — #188's own scope limits Bogus to
    /// non-semantic demo data, and a question's text/options/correct answer are exactly the kind of
    /// content that has to actually be correct, not just plausible-looking.
    /// <see cref="Questions"/> is this bank's fixed, deterministic order; <see cref="Shuffle"/> is the
    /// seeded alternative — the same seed always produces the same ordering, so a game replay or a
    /// test can reproduce an exact sequence of questions.
    /// </summary>
    internal
#if !DEBUG
        sealed
#endif
        class QuizQuestionBank
    {
        /// <summary>Minimum number of questions a bank must contain — #188's own AC.</summary>
        public const int MinimumQuestionCount = 20;

        private readonly IReadOnlyList<QuizQuestion> _questions;

        public QuizQuestionBank(IReadOnlyList<QuizQuestion> questions)
        {
            if (questions is null || questions.Count < MinimumQuestionCount)
                throw new QuizQuestionValidationException($"a question bank must contain at least {MinimumQuestionCount} questions.");

            _questions = questions;
        }

        public int Count => _questions.Count;

        /// <summary>This bank's fixed, deterministic order — the order its questions were supplied in.</summary>
        public IReadOnlyList<QuizQuestion> Questions => _questions;

        /// <summary>
        /// A Fisher-Yates shuffle of every question in this bank, seeded by <paramref name="seed"/>:
        /// the same seed always yields the same resulting order, on any machine, any run. Does not
        /// mutate <see cref="Questions"/>' own order.
        /// </summary>
        public IReadOnlyList<QuizQuestion> Shuffle(int seed)
        {
            var shuffled = _questions.ToArray();
            var random = new Random(seed);

            for (var i = shuffled.Length - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }

            return shuffled;
        }

        /// <summary>The demo's default built-in question set — real trivia, not generated filler.</summary>
        public static QuizQuestionBank CreateDefault() => new(
        [
            new QuizQuestion("What is the capital of France?", ["Paris", "London", "Berlin", "Madrid"], 0),
            new QuizQuestion("What is the capital of Japan?", ["Seoul", "Beijing", "Tokyo", "Bangkok"], 2),
            new QuizQuestion("Which planet is known as the Red Planet?", ["Venus", "Mars", "Jupiter", "Saturn"], 1),
            new QuizQuestion("What is the largest ocean on Earth?", ["Atlantic", "Indian", "Arctic", "Pacific"], 3),
            new QuizQuestion("Who wrote the play 'Romeo and Juliet'?", ["Charles Dickens", "William Shakespeare", "Mark Twain", "Jane Austen"], 1),
            new QuizQuestion("What is the chemical symbol for gold?", ["Ag", "Au", "Gd", "Go"], 1),
            new QuizQuestion("How many continents are there on Earth?", ["5", "6", "7", "8"], 2),
            new QuizQuestion("What is the tallest mountain in the world?", ["K2", "Kangchenjunga", "Mount Everest", "Lhotse"], 2),
            new QuizQuestion("Which gas do plants primarily absorb from the atmosphere?", ["Oxygen", "Nitrogen", "Carbon dioxide", "Hydrogen"], 2),
            new QuizQuestion("What is the smallest prime number?", ["0", "1", "2", "3"], 2),
            new QuizQuestion("Which country is home to the kangaroo?", ["South Africa", "Brazil", "India", "Australia"], 3),
            new QuizQuestion("What is the freezing point of water in Celsius?", ["0", "32", "100", "-1"], 0),
            new QuizQuestion("Who painted the Mona Lisa?", ["Vincent van Gogh", "Pablo Picasso", "Leonardo da Vinci", "Claude Monet"], 2),
            new QuizQuestion("What is the largest planet in our solar system?", ["Earth", "Saturn", "Neptune", "Jupiter"], 3),
            new QuizQuestion("Which language has the most native speakers worldwide?", ["English", "Hindi", "Mandarin Chinese", "Spanish"], 2),
            new QuizQuestion("What is the currency of Japan?", ["Won", "Yuan", "Yen", "Ringgit"], 2),
            new QuizQuestion("How many legs does a spider have?", ["6", "8", "10", "12"], 1),
            new QuizQuestion("What is the longest river in the world?", ["Amazon", "Nile", "Yangtze", "Mississippi"], 1),
            new QuizQuestion("In which year did World War II end?", ["1943", "1944", "1945", "1946"], 2),
            new QuizQuestion("What is the hardest natural substance on Earth?", ["Gold", "Diamond", "Quartz", "Iron"], 1),
            new QuizQuestion("Which organ pumps blood through the human body?", ["Liver", "Lungs", "Heart", "Kidneys"], 2),
            new QuizQuestion("What is the main ingredient in guacamole?", ["Tomato", "Avocado", "Onion", "Lime"], 1),
            new QuizQuestion("Which planet is closest to the Sun?", ["Venus", "Earth", "Mercury", "Mars"], 2),
            new QuizQuestion("What is the square root of 64?", ["6", "7", "8", "9"], 2),
        ]);
    }
}
