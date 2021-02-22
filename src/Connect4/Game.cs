using System;
using System.Linq;

namespace Connect4
{
    public record Name(string FirstName, string? MiddleName, string LastName)
    {
        public override string ToString() => string.IsNullOrWhiteSpace(MiddleName) switch
        {
            true  => $"{FirstName} {LastName}",
            false => $"{FirstName} {MiddleName} {LastName}"
        };
    }
    public record Person(Name Name);
    public record PlayerColor;
    public record Yellow : PlayerColor;
    public record Red : PlayerColor;
    public record Player(Person Person, PlayerColor Color) : Person(Person.Name);
    public record Challenger(Person Person) : Player(Person, new Yellow());
    public record Opponent(Person Person) : Player(Person, new Red());
    public record Column(ValidColumnNumber ColumnNumber);
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
        public static void AllMethods()
        {
            NewGame CreateNewGame(Challenger challenger, Opponent opponent) => new NewGame(challenger, opponent);

            Choice<Game?, WonGame, DrawGame> TryMove(Game game, Column column)
            {
                var player = game.NextMove;

                var newBoard = TryPlayDisk(player, game.Board, column);
                if (newBoard == null)
                    return default(Game?);
                
                return newBoard.Item switch
                {
                    WinningBoard wb => new WonGame(player, GetNextPlayer(game), wb),
                    DrawBoard db    => new DrawGame(game.Challenger, game.Opponent, db),
                    Board b         => game with 
                                    { 
                                        Board = b,
                                        NextMove = GetNextPlayer(game)
                                    },
                    _               => throw new Exception("Invalid input for board choice.")
                };

                Choice<Board?, WinningBoard, DrawBoard> TryPlayDisk(Player player, Board board, Column column)
                { 
                    if(!CanPlayDisk(board, column))
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

                    bool CanPlayDisk(Board board, Column column) => 
                        board.BoardState[column.ColumnNumber.ColumnNumber - 1, 0] == 0; // "top" slot is empty
                }

                Player GetNextPlayer(Game game) => game.NextMove == game.Challenger ? game.Opponent : game.Challenger;

                int[,] UpdateBoardState(int[,] boardState, Column column, PlayerColor color)
                {
                    var firstOpenRow = boardState.SliceColumn(column.ColumnNumber.ColumnNumber - 1).Where(row => row != 0).Count();
                    boardState[column.ColumnNumber.ColumnNumber - 1, firstOpenRow] = GetColorIndex(color); // even better if immutable (or an array of Columns)
                    return boardState;
                }

                bool IsWinningBoardState(int[,] boardState) => true; // todo: add logic

                bool IsDrawBoardState(int[,] boardState) => true; // todo: add logic
            
                int GetColorIndex(PlayerColor color) => color switch { Yellow => 1, Red => 2, _ => throw new Exception($"Color must be {nameof(Yellow)} or {nameof(Red)}") };
            }

            var challenger = new Challenger(new Person(new Name("David", "John", "Cuccia")));
            var opponent = new Opponent(new Person(new Name("Sara", "Joyce", "Robinson")));
            Game game = CreateNewGame(challenger, opponent);
            Choice<WonGame, DrawGame>? finishedGame = null;
            do
            {
                var columnToPlay = GetNextRandomMove(game.Board);
                var maybeUpdatedGame = TryMove(game, columnToPlay);
                if(maybeUpdatedGame == null)
                {
                    System.Console.WriteLine("Invalid move. Please try again.");
                    continue;
                }
                if(maybeUpdatedGame.Item is WonGame || maybeUpdatedGame.Item is DrawGame)
                {
                    finishedGame = maybeUpdatedGame.Item;
                }
            } while(finishedGame == null);

            if(finishedGame.Item is WonGame wonGame)
                System.Console.WriteLine($"Congratulations to the {wonGame.Winner.Color} player, {wonGame.Winner.Name}!");
            else if(finishedGame.Item is DrawGame drawGame)
                System.Console.WriteLine($"Draw! Better luck next time to {drawGame.Challenger.Name} and {drawGame.Opponent.Name}.");

            Column GetNextRandomMove(Board board)
            {
                Column? column;
                do
                {
                    column = TryGetColumn(_rng.Next(1,7));
                } while(column == null || board.BoardState[column.ColumnNumber.ColumnNumber - 1, 0] != 0);
                
                return column;            
                
                Column? TryGetColumn(int columnNumber) => columnNumber switch
                {
                    >= 1 and <= 7 => new Column(new ValidColumnNumber(columnNumber)),
                    _             => null
                };
            }
        }
    }
}
