using System;
using System.Collections.Generic;

namespace Invaders.Tetris
{
    public class TetrisPieceBag
    {
        private readonly Queue<TetrominoType> m_Queue = new Queue<TetrominoType>();
        private readonly Random m_Random = new Random();

        public TetrisPieceBag()
        {
            refillBag();
            refillBag();
        }

        public Tetromino NextPiece()
        {
            if (m_Queue.Count <= 7)
            {
                refillBag();
            }

            TetrominoType nextType = m_Queue.Dequeue();
            return new Tetromino(nextType);
        }

        public TetrominoType[] PeekNext(int i_Count)
        {
            while (m_Queue.Count < i_Count)
            {
                refillBag();
            }

            TetrominoType[] array = m_Queue.ToArray();
            TetrominoType[] result = new TetrominoType[Math.Min(i_Count, array.Length)];
            Array.Copy(array, result, result.Length);
            return result;
        }

        public void Reset()
        {
            m_Queue.Clear();
            refillBag();
            refillBag();
        }

        private void refillBag()
        {
            TetrominoType[] pieces = (TetrominoType[])Enum.GetValues(typeof(TetrominoType));
            // Fisher-Yates shuffle
            for (int i = pieces.Length - 1; i > 0; i--)
            {
                int j = m_Random.Next(i + 1);
                TetrominoType temp = pieces[i];
                pieces[i] = pieces[j];
                pieces[j] = temp;
            }

            foreach (TetrominoType piece in pieces)
            {
                m_Queue.Enqueue(piece);
            }
        }
    }
}

