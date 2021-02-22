using System;
using System.Linq;
using System.Text;

namespace Connect4
{
    public record Name(string FirstName, string? MiddleName, string LastName);
    public record Person(Name Name);
    public record PlayerColor;
    public record Yellow : PlayerColor;
    public record Red : PlayerColor;
    public record Player(Person Person, PlayerColor Color) : Person(Person.Name);
    public record Challenger(Person Person) : Player(Person, new Yellow());
    public record Opponent(Person Person) : Player(Person, new Red());
    public record Column
    {
        public int ColumnNumber { get; }
        public Column(ValidColumnNumber columnNumber) { ColumnNumber = columnNumber.ColumnNumber; }
    }
    public record ValidColumnNumber(int ColumnNumber);
    public record Board(int[,] BoardState);
    public record WinningBoard(int[,] BoardState) : Board(BoardState);
    public record DrawBoard(int[,] BoardState) : Board(BoardState);
    public record EmptyBoard() : Board(new int[7,6]);
    public record Game(Board Board, Challenger Challenger, Opponent Opponent, Player NextMove);
    public record NewGame(Challenger Challenger, Opponent Opponent) : Game(new EmptyBoard(), Challenger, Opponent, Challenger);
    public record WonGame(Player Winner, Player Loser, WinningBoard WinningBoard);
    public record DrawGame(Player Challenger, Player Opponent, DrawBoard DrawBoard);

    public static class GameMethods
    {
        private static readonly Random _rng = new ();

        public static (Choice<Game, WonGame, DrawGame>, string message) Move(this Game game, Column column)
        {
            var player = game.NextMove;

            var newBoard = TryDropDisk(player, game.Board, column);
            if (newBoard == null)
                return (game, $"Invalid move. Please try again {player.GetPrettyName()}.");

            Choice<Game, WonGame, DrawGame> newGame = newBoard.Item switch
            {
                WinningBoard wb => new WonGame(player, GetNextPlayer(game), wb),
                DrawBoard db    => new DrawGame(game.Challenger, game.Opponent, db),
                Board b         => game with { Board = b, NextMove = GetNextPlayer(game) },
                _               => throw new Exception("Invalid input for board choice.")
            };

            return newGame.Item switch
            {
                WonGame wg  => (wg, $"Winner! Congratulations to the {wg.Winner.Color} player, {wg.Winner.GetPrettyName()}!"),
                DrawGame dg => (dg, $"Draw! Better luck next time to {dg.Challenger.GetPrettyName()} and {dg.Opponent.GetPrettyName()}."),
                Game g      => (g, $"{g.NextMove.Color} moves to Column {column.ColumnNumber}"),
                _           => throw new Exception("Invalid input for board choice.")
            };

            Choice<Board?, WinningBoard, DrawBoard> TryDropDisk(Player player, Board board, Column column)
            { 
                if(!CanDropDisk(board, column))
                    return default(Board?);

                var newBoardState = UpdateBoardState(board.BoardState, column, player.Color);

                return IsWinningBoardState(newBoardState) switch
                {
                    true  => new WinningBoard(newBoardState),
                    false => IsDrawBoardState(newBoardState) switch
                            {
                                true  => new DrawBoard(newBoardState),
                                false => board with { BoardState = newBoardState }
                            }
                };

                bool CanDropDisk(Board board, Column column) => board.BoardState[column.ColumnNumber - 1, 0] == 0; // "top" slot is empty
            }

            Player GetNextPlayer(Game game) => game.NextMove == game.Challenger ? game.Opponent : game.Challenger;

            int[,] UpdateBoardState(int[,] boardState, Column column, PlayerColor color)
            {
                var firstOpenRow = boardState.SliceColumn(column.ColumnNumber - 1).Where(row => row != 0).Count();
                boardState[column.ColumnNumber - 1, firstOpenRow] = GetColorIndex(color); // even better if immutable (or an array of Columns)
                return boardState;
            }

            bool IsWinningBoardState(int[,] boardState) => true; // todo: add logic

            bool IsDrawBoardState(int[,] boardState) => true; // todo: add logic
        
            int GetColorIndex(PlayerColor color) => color switch { Yellow => 1, Red => 2, _ => throw new Exception($"Color must be {nameof(Yellow)} or {nameof(Red)}") };
        }

        public static void Demo()
        {
            var challenger = new Challenger(new Person(new Name("David", "John", "Cuccia")));
            var opponent = new Opponent(new Person(new Name("Sara", "Joyce", "Robinson")));

            Game game = new NewGame(challenger, opponent);

            var finishedGame = PlayUntilDone(game);

            PrintGameResult(finishedGame);

            Choice<WonGame, DrawGame> PlayUntilDone(Game game)
            {
                while(true)
                {
                    var columnToPlay = GetNextRandomMove(game.Board);

                    (var updatedGame, var message) = game.Move(columnToPlay);

                    Console.WriteLine(message);

                    if (updatedGame.Item is WonGame wg) return wg;
                    if (updatedGame.Item is DrawGame dg) return dg;
                } 
            }

            Column GetNextRandomMove(Board board)
            {
                Column? column = null;
                while(column == null || board.BoardState[column.ColumnNumber - 1, 0] != 0)
                    column = TryGetColumn(_rng.Next(1,7));
                
                return column;            
                
                Column? TryGetColumn(int columnNumber) => columnNumber switch
                {
                    >= 1 and <= 7 => new Column(new ValidColumnNumber(columnNumber)),
                    _             => null
                };
            }

            void PrintGameResult(Choice<WonGame, DrawGame> finishedGame)
            {
                var finishedBoardState = finishedGame.Item switch
                {
                    WonGame wg => wg.WinningBoard.BoardState,
                    DrawGame dg => dg.DrawBoard.BoardState,
                    _ => throw new Exception()
                };

                Console.WriteLine(finishedBoardState.GetFormattedBoardString());
            };
        }

        public static string GetPrettyName(this Player player) => string.IsNullOrWhiteSpace(player.Name.MiddleName) switch
        {
            true  => $"{player.Name.FirstName} {player.Name.LastName}",
            false => $"{player.Name.FirstName} {player.Name.MiddleName} {player.Name.LastName}"
        };

        public static string GetFormattedBoardString(this int[,] boardState)
        {
            if (boardState == null)
                return "";
            StringBuilder sb = new();
            for (int i = 0; i < boardState.GetLength(0); i++)
            {
                for (int j = 0; j < boardState.GetLength(1); j++)
                {
                    sb.Append(boardState[i, j] + "\t");
                }
                sb.Append("\n");
            }
            return sb.ToString();
        }
    }
}
