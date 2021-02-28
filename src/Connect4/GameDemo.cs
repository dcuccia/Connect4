using System;

namespace Connect4
{
    public class GameDemo
    {
        private readonly Random _rng = new();

        public void Run()
        {
            var challenger = new Challenger(new Name("David", "John", "Cuccia"));
            var opponent = new Opponent(new Name("Sara", "Joyce", "Robinson"));

            Game game = new NewGame(challenger, opponent);

            var finishedGame = PlayUntilDone(game);

            Choice<WonGame, DrawGame> PlayUntilDone(Game game)
            {
                while (true)
                {
                    var columnToPlay = GetNextRandomMove(game.Board);

                    (var updatedGame, var message, var boardString) = game.Move(columnToPlay); // example record decomp benefit!

                    Console.WriteLine(message);
                    Console.WriteLine(boardString);

                    if (updatedGame.Item is WonGame wg) return wg;
                    if (updatedGame.Item is DrawGame dg) return dg;
                    if (updatedGame.Item is Game g) game = g;
                }
            }

            Column GetNextRandomMove(Board board)
            {
                Column? column = null;
                while (column == null || board.BoardState[column.ColumnNumber - 1, Globals.HEIGHT - 1] != 0)
                    column = TryGetColumn(_rng.Next(1, Globals.WIDTH + 1));
                return column;

                Column? TryGetColumn(int columnNumber) => columnNumber switch
                {
                    >= 1 and <= Globals.WIDTH => new Column(new ValidColumnNumber(columnNumber)),
                    _ => null
                };
            }
        }
    }
}
