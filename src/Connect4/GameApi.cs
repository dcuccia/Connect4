
using System.Text;
using OneOf;
namespace Connect4;

public static class Globals { public const int WIDTH = 7; public const int HEIGHT = 6; }

public record Name(string FirstName, string LastName) { public override string ToString() => $"{FirstName} {LastName}"; }
public record Orange { public override string ToString() => nameof(Orange); }
public record Blue { public override string ToString() => nameof(Blue); }
[GenerateOneOf]
public partial class PlayerColor : OneOfBase<Orange, Blue> { }
public record Player(Name Name, PlayerColor PlayerColor);
public record Challenger(Name Name) : Player(Name, new Orange());
public record Opponent(Name Name) : Player(Name, new Blue());
public record Column
{
    public int ColumnNumber { get; }
    public Column(ValidColumnNumber columnNumber) { ColumnNumber = columnNumber.ColumnNumber; }
}
public record ValidColumnNumber(int ColumnNumber);
public record Board(int[,] BoardState);
public record WinningBoard(int[,] BoardState) : Board(BoardState);
public record DrawBoard(int[,] BoardState) : Board(BoardState);
public record InvalidMove;
public record Game(Board Board, Challenger Challenger, Opponent Opponent, Player NextMove);
public record NewGame(Challenger Challenger, Opponent Opponent) : Game(new Board(new int[Globals.WIDTH, Globals.HEIGHT]), Challenger, Opponent, Challenger);
public record WonGame(Player Winner, Player Loser, WinningBoard WinningBoard);
public record DrawGame(Player Challenger, Player Opponent, DrawBoard DrawBoard);
public record GameTurn(OneOf<Game, WonGame, DrawGame> Game, string TurnMessage); // example enhancement

public static class GameMethods
{
    public static GameTurn Move(this Game game, Column column)
    {
        var player = game.NextMove;

        var newBoard = TryDropDisk(player, game.Board, column);
        if (newBoard.Value is InvalidMove)
            return new GameTurn(game, $"Invalid move. Please try again {player.Name}.");

        OneOf<Game, WonGame, DrawGame> newGame = newBoard.Value switch
        {
            WinningBoard wb => new WonGame(player, GetNextPlayer(game), wb),
            DrawBoard db    => new DrawGame(game.Challenger, game.Opponent, db),
            Board b         => game with { Board = b, NextMove = GetNextPlayer(game) },
            _               => throw new Exception("Invalid input for board choice.")
        };

        return newGame.Value switch
        {
            WonGame wg  => new GameTurn(wg, $"Winner! Congratulations to the {wg.Winner.PlayerColor.Value} player, {wg.Winner.Name}!"),
            DrawGame dg => new GameTurn(dg, $"Draw! Better luck next time to {dg.Challenger.Name} and {dg.Opponent.Name}."),
            Game g      => new GameTurn(g, $"{g.NextMove.PlayerColor.Value} moves to Column {column.ColumnNumber}"),
            _           => throw new Exception("Invalid input for board choice.")
        };

        OneOf<Board, WinningBoard, DrawBoard, InvalidMove> TryDropDisk(Player player, Board board, Column column)
        {
            if (!CanDropDisk(board, column))
                return new InvalidMove();

            var newBoardState = UpdateBoardState(board.BoardState, column, player.PlayerColor);

            return IsWinningBoardState(newBoardState) switch
            {
                true  => new WinningBoard(newBoardState),
                false => IsDrawBoardState(newBoardState) switch
                         {
                             true  => new DrawBoard(newBoardState),
                             false => board with { BoardState = newBoardState }
                         }
            };

            bool CanDropDisk(Board board, Column column) => board.BoardState[column.ColumnNumber - 1, board.BoardState.GetLength(1) - 1] == 0; // "top" slot is empty
        }

        Player GetNextPlayer(Game game) => game.NextMove.Name == game.Challenger.Name ? game.Opponent : game.Challenger; // inheritance equality is very literal with records

        int[,] UpdateBoardState(int[,] boardState, Column column, PlayerColor color)
        {
            var firstOpenRow = boardState.SliceColumn(column.ColumnNumber - 1).Where(row => row != 0).Count();
            boardState[column.ColumnNumber - 1, firstOpenRow] = GetColorIndex(color); // even better if immutable (or an array of Columns)
            return boardState;
        }

        bool IsWinningBoardState(int[,] boardState)
        {
            var width = boardState.GetLength(0);
            var height = boardState.GetLength(1);
            // check rows
            for (int j = 0; j < height; j++)
            {
                for (int i = 0; i < width - 3; i++)
                {
                    var num = boardState[i, j];
                    if (num > 0 && boardState[i + 1, j] == num && boardState[i + 2, j] == num && boardState[i + 3, j] == num)
                        return true;
                }
            }
            // check columns
            for (int j = 0; j < height - 3; j++)
            {
                for (int i = 0; i < width; i++)
                {
                    var num = boardState[i, j];
                    if (num > 0 && boardState[i, j + 1] == num && boardState[i, j + 2] == num && boardState[i, j + 3] == num)
                        return true;
                }
            }
            // check "up-right" diagonals
            for (int j = 0; j < height - 3; j++)
            {
                for (int i = 0; i < width - 3; i++)
                {
                    var num = boardState[i, j];
                    if (num > 0 && boardState[i + 1, j + 1] == num && boardState[i + 2, j + 2] == num && boardState[i + 3, j + 3] == num)
                        return true;
                }
            }
            // check "up-left" diagonals
            for (int j = 0; j < height - 3; j++)
            {
                for (int i = 3; i < width; i++)
                {
                    var num = boardState[i, j];
                    if (num > 0 && boardState[i - 1, j + 1] == num && boardState[i - 2, j + 2] == num && boardState[i - 3, j + 3] == num)
                        return true;
                }
            }
            return false;
        }

        // for now, simply define a draw state as a full board (even if winning paths no longer exist)
        bool IsDrawBoardState(int[,] boardState)
        {
            var width = boardState.GetLength(0);
            var height = boardState.GetLength(1);
            for (int i = 0; i < width; i++)
                if (boardState[i, height - 1] == 0)
                    return false;
            return true;
        }

        int GetColorIndex(PlayerColor color) => color.Value switch { Orange => 1, Blue => 2, _ => throw new Exception($"Color must be {nameof(Orange)} or {nameof(Blue)}") };

    }

    public static string GetFormattedBoardStateString(this Game game)     => ((OneOf<Game, WonGame, DrawGame>)game).GetFormattedBoardStateString();
    public static string GetFormattedBoardStateString(this WonGame game)  => ((OneOf<Game, WonGame, DrawGame>)game).GetFormattedBoardStateString();
    public static string GetFormattedBoardStateString(this DrawGame game) => ((OneOf<Game, WonGame, DrawGame>)game).GetFormattedBoardStateString();
    public static string GetFormattedBoardStateString(this OneOf<Game, WonGame, DrawGame> game)
    {
        var boardState = game.Value switch
        {
            WonGame wg => wg.WinningBoard.BoardState,
            DrawGame dg => dg.DrawBoard.BoardState,
            Game g => g.Board.BoardState,
            _ => throw new Exception()
        };

        StringBuilder sb = new();
        for (int j = boardState.GetLength(1) - 1; j >= 0; j--)
        {
            for (int i = 0; i < boardState.GetLength(0); i++)
            {
                var stateChar = boardState[i, j] > 0 ? boardState[i, j].ToString() : "_";
                sb.Append(stateChar + "\t");
            }
            sb.Append("\n");
        }
        return sb.ToString();
    }
}
