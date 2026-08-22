using ThunderPropagator.Channels.Demo.Quiz.Game;
using ThunderPropagator.Channels.Demo.Quiz.Game.Exceptions;

namespace ThunderPropagator.UnitTests.Demo.Quiz.Game
{
    /// <summary>
    /// Issue #188: covers QuizQuestionBank's own contract — CreateDefault supplies at least the
    /// required minimum of valid questions, a bank rejects being constructed with too few, its
    /// natural order is stable, and Shuffle is deterministic per seed (the AC's "seeded selection
    /// produces repeatable sequences") while still being a genuine reordering across seeds and never
    /// mutating the bank's own order.
    /// </summary>
    public sealed class QuizQuestionBankTests
    {
        private static QuizQuestion Question(string text) => new(text, ["A", "B"], 0);

        private static IReadOnlyList<QuizQuestion> ValidQuestions(int count)
            => Enumerable.Range(0, count).Select(index => Question($"Question {index}?")).ToArray();

        [Fact]
        public void CreateDefault_HasAtLeastTheMinimumQuestionCount()
        {
            var bank = QuizQuestionBank.CreateDefault();

            Assert.True(bank.Count >= QuizQuestionBank.MinimumQuestionCount);
        }

        [Fact]
        public void CreateDefault_EveryQuestionsCorrectAnswerIsAmongItsOptions()
        {
            var bank = QuizQuestionBank.CreateDefault();

            Assert.All(bank.Questions, question => Assert.Contains(question.CorrectAnswer, question.Options));
        }

        [Fact]
        public void Constructor_WithFewerThanTheMinimumQuestionCount_Throws()
        {
            var exception = Record.Exception(() => new QuizQuestionBank(ValidQuestions(QuizQuestionBank.MinimumQuestionCount - 1)));

            Assert.IsType<QuizQuestionValidationException>(exception);
        }

        [Fact]
        public void Constructor_WithNullQuestions_Throws()
        {
            var exception = Record.Exception(() => new QuizQuestionBank(null!));

            Assert.IsType<QuizQuestionValidationException>(exception);
        }

        [Fact]
        public void Constructor_WithExactlyTheMinimumQuestionCount_Succeeds()
        {
            var bank = new QuizQuestionBank(ValidQuestions(QuizQuestionBank.MinimumQuestionCount));

            Assert.Equal(QuizQuestionBank.MinimumQuestionCount, bank.Count);
        }

        [Fact]
        public void Questions_ReturnsTheSuppliedOrder()
        {
            var questions = ValidQuestions(QuizQuestionBank.MinimumQuestionCount);
            var bank = new QuizQuestionBank(questions);

            Assert.Equal(questions, bank.Questions);
        }

        [Fact]
        public void Shuffle_WithTheSameSeed_ProducesTheSameOrderEveryTime()
        {
            var bank = new QuizQuestionBank(ValidQuestions(QuizQuestionBank.MinimumQuestionCount));

            var first = bank.Shuffle(42);
            var second = bank.Shuffle(42);

            Assert.Equal(first, second);
        }

        [Fact]
        public void Shuffle_WithDifferentSeeds_ProducesDifferentOrders()
        {
            var bank = new QuizQuestionBank(ValidQuestions(QuizQuestionBank.MinimumQuestionCount));

            var first = bank.Shuffle(1);
            var second = bank.Shuffle(2);

            Assert.NotEqual(first, second);
        }

        [Fact]
        public void Shuffle_ContainsExactlyTheSameQuestionsAsTheOriginalOrder()
        {
            var bank = new QuizQuestionBank(ValidQuestions(QuizQuestionBank.MinimumQuestionCount));

            var shuffled = bank.Shuffle(7);

            Assert.Equal(bank.Questions.ToHashSet(), shuffled.ToHashSet());
        }

        [Fact]
        public void Shuffle_DoesNotMutateTheBanksOwnOrder()
        {
            var questions = ValidQuestions(QuizQuestionBank.MinimumQuestionCount);
            var bank = new QuizQuestionBank(questions);

            bank.Shuffle(99);

            Assert.Equal(questions, bank.Questions);
        }
    }
}
