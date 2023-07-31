using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ChessWithConnect4;



public enum ChessPieceType {Pawn, Bishop, Queen, Knight, Rook, King}


public static class ChessPieceExtensions
{
    public static Move[] GetMoves(this ChessPieceType piece, Position currentPosition)
    {
        switch (piece)
        {
            case ChessPieceType.Pawn: // TODO: implement promotion
            {
                if (currentPosition.Y == Game.MaxY)
                {
                    return Array.Empty<Move>();
                }
                var positionAbove = currentPosition with {Y = currentPosition.Y + 1};
                var moveOneAbove =
                    new Move(positionAbove,
                        new[] {positionAbove});

                const int lastYForDoubleMove = 1;
                if (currentPosition.Y > lastYForDoubleMove)
                    return new[]
                    {
                        moveOneAbove
                    };
                
                var positionTwoAbove = currentPosition with {Y = currentPosition.Y + 2};
                var moveTwoAbove = new Move(
                    positionTwoAbove,
                    new[] {positionAbove, positionTwoAbove}
                );
                
                return new[]
                {
                    moveOneAbove,
                    moveTwoAbove
                };
            }
            case ChessPieceType.Bishop:
            {
                var directionIncrements = new Position[]
                {
                    new(-1, -1),
                    new(-1, 1),
                    new(1, -1),
                    new(1, 1)
                };
                var allPotentialMovesByDirection = directionIncrements.Select(directionIncrement =>
                    InfiniteRangeFrom(1).Select(iterations => 
                        new Move(
                            Position: currentPosition + directionIncrement * iterations, 
                            PositionsRequiredToBeFree: Enumerable
                                .Range(1, iterations)
                                .Select(i => currentPosition +  directionIncrement * i).ToArray()
                        )
                    )
                ).ToArray();
                return allPotentialMovesByDirection.SelectMany(potentialMovesInOneDirection =>
                        potentialMovesInOneDirection.TakeWhile(potentialMove => potentialMove.Position.IsOnBoard())
                    )
                    .ToArray();
            }
            case ChessPieceType.Queen:
            {
                return ChessPieceType.Bishop.GetMoves(currentPosition)
                    .Concat(ChessPieceType.Rook.GetMoves(currentPosition))
                    .ToArray();
            }
            case ChessPieceType.Knight:
            {
                var offsets = new Position[]
                {
                    new(2, 1),
                    new(2, -1),
                    new(-2, -1),
                    new(-2, 1),
                    new(1, 2),
                    new(1, -2),
                    new(-1, -2),
                    new(-1, 2)
                };
                var positions = offsets
                    .Select(offset => offset + currentPosition)
                    .Where(position => position.IsOnBoard());
                return positions
                    .Select(position => new Move(position, new[] {position}))
                    .ToArray();
            }
            case ChessPieceType.Rook:
            {
                var directionIncrements = new Position[]
                {
                    new(0, -1),
                    new(0, 1),
                    new(-1, 0),
                    new(1, 0)
                };
                var allPotentialMovesByDirection = directionIncrements.Select(directionIncrement =>
                    InfiniteRangeFrom(1).Select(iterations => 
                        new Move(
                            Position: currentPosition + directionIncrement * iterations, 
                            PositionsRequiredToBeFree: Enumerable
                                .Range(1, iterations)
                                .Select(i => currentPosition +  directionIncrement * i).ToArray()
                            )
                    )
                ).ToArray();
                return allPotentialMovesByDirection.SelectMany(potentialMovesInOneDirection =>
                        potentialMovesInOneDirection.TakeWhile(potentialMove => potentialMove.Position.IsOnBoard())
                    )
                    .ToArray();
            }
            case ChessPieceType.King:  // TODO: implement castling
            {
                var offsets = new Position[]
                {
                    new(0, -1),
                    new(0, 1),
                    new(1, -1),
                    new(1, 0),
                    new(1, 1),
                    new(-1, -1),
                    new(-1, 0),
                    new(-1, 1)
                };
                var positions = offsets
                    .Select(offset => offset + currentPosition)
                    .Where(position => position.IsOnBoard());
                return positions
                    .Select(position => new Move(position, new[] {position}))
                    .ToArray();
            }
            default:
            {
                throw new ArgumentOutOfRangeException(nameof(piece), piece, null);
            }
        }
    }
    private static IEnumerable<int> InfiniteRangeFrom(int start)
    {
        int counter = start;
        while (true)
        {
            unchecked
            {
                yield return counter;
                counter++;
            }
        }
    }
}

public record Move(Position Position, Position[] PositionsRequiredToBeFree);

public readonly record struct Position(int X, int Y)
{
    public bool IsOnBoard()
    {
        return X >= 0  && X <= Game.MaxX && Y >= 0 && Y <= Game.MaxY;
    }
    public static Position operator +(Position a, Position b)
        => new Position(a.X + b.X, a.Y + b.Y);
    public static Position operator *(Position a, int times)
        => new Position(a.X * times, a.Y * times);
}