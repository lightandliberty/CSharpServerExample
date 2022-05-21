using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirusWarGameServer
{
    public static class CHelper
    {
        static byte COLUMN_COUNT = 7;

        /// <summary>
        /// 포지션(1차원 배열)을 (row, col)형식의 좌표로 변환한다.
        /// </summary>
        /// <param name="cell"></param>
        /// <returns></returns>
        static Vector2 Convert_To_XY(short position)
        {
            return new Vector2(Calc_Row(position), Calc_Col(position));
        }

        /// <summary>
        /// (row, col)형식의 좌표를 포지션으로 변환한다.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public static short Get_Position(byte row, byte col)
        {
            return (short)(row * COLUMN_COUNT * col);
        }


        /// <summary>
        /// 포지션으로부터 세로 인덱스를 구한다.
        /// </summary>
        /// <param name="cell"></param>
        /// <returns></returns>
        public static short Calc_Row(short position)
        {
            return (short)(position / COLUMN_COUNT);
        }

        /// <summary>
        /// 포지션으로부터 가로 인덱스를 구한다.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public static short Calc_Col(short position)
        {
            return (short)(position % COLUMN_COUNT);
        }

        /// <summary>
        /// cell 인덱스를 넣으면, 둘 사이의 거리값을 리턴해 준다.
        /// 한 칸이 차이나면 1, 두 칸이 차이나면 2
        /// 1차원 배열 from과 to를 Vector2로 변환 후, 가로나 세로 값중 더 차이나는 거리를 반환. 
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static short Get_Distance(short from, short to)
        {
            
            Vector2 pos1 = Convert_To_XY(from);
            Vector2 pos2 = Convert_To_XY(to);
            return Get_Distance(pos1, pos2);

        }


        /// <summary>
        /// 가로나 세로 중, 더 먼거리를 리턴.
        /// </summary>
        /// <param name="pos1"></param>
        /// <param name="pos2"></param>
        /// <returns></returns>
        public static short Get_Distance(Vector2 pos1, Vector2 pos2)
        {
            Vector2 distance = pos1 - pos2;
            short x = (short)Math.Abs(distance.x);  // Math.Abs 절대값을 반환
            short y = (short)Math.Abs(distance.y);

            // x,y중 큰 값이 실제 두 위치 사이의 거리를 뜻한다.
            return Math.Max(x, y);
        }

        /// <summary>
        /// 가로나 세로 중 더 먼 거리를 리턴.
        /// </summary>
        /// <param name="basis_cell"></param>
        /// <param name="cell"></param>
        /// <returns></returns>
        public static byte HowFar_From_Clicked_Cell(short basis_cell, short cell)
        {
            short row = (short)(basis_cell / COLUMN_COUNT);
            short col = (short)(basis_cell % COLUMN_COUNT);
            Vector2 basis_pos = new Vector2(col, row);

            row = (short)(cell / COLUMN_COUNT);
            col = (short)(cell % COLUMN_COUNT);
            Vector2 cell_pos = new Vector2(col, row);

            Vector2 distance = (basis_pos - cell_pos);
            short x = (short)Math.Abs(distance.x);
            short y = (short)Math.Abs(distance.y);
            return (byte)Math.Max(x, y);
        }

        /// <summary>
        /// 주위에 있는 셀의 위치를 찾아서, 리스트로 리턴해 준다.
        /// </summary>
        /// <param name="basis_cell"></param>
        /// <param name="targets"></param>
        /// <param name="gap"></param>
        /// <returns></returns>
        public static List<short> Find_Neighbor_Cells(short basis_cell, List<short> targets, short gap)
        {
            Vector2 pos = Convert_To_XY(basis_cell);
            // 조건에 맞는 요소의 배열을 리턴.
            return targets.FindAll(obj => Get_Distance(pos, Convert_To_XY(obj)) <= gap);   // targets배열에 있는 원소와 basis_cell과의 거리가 gap보다 작거나 같은 targets원소들 리턴.
        }

        /// <summary>
        /// 게임을 지속할 수 있는지 체크한다.
        /// </summary>
        /// <param name="board"></param>
        /// <param name="players"></param>
        /// <param name="current_player_index"></param>
        /// <returns></returns>
        public static bool Can_Play_More(List<short> board, CPlayer current_player, List<CPlayer> all_player)
        {
            foreach(short cell in current_player.viruses)
            {
                // 이동가능한 셀의 개수가 0보다 크면, true 아니면 false(게임을 지속할 수 없음)
                if(CHelper.Find_Available_Cells(cell, board, all_player).Count > 0)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 이동 가능한 셀을 찾아서 리스트 배열로 리턴
        /// </summary>
        /// <param name="basis_cell"></param>
        /// <param name="total_cells"></param>
        /// <param name="players"></param>
        /// <returns></returns>
        public static List<short> Find_Available_Cells(short basis_cell, List<short> total_cells, List<CPlayer> players)
        {
            // 주변에 있는 셀의 리스트에서,
            List<short> targets = Find_Neighbor_Cells(basis_cell, total_cells, 2);

            players.ForEach(obj =>
            {
                // 주변에 있는 셀의 배열에서 바이러스들이 있는 셀의 번호와 같은 셀을 제거하면, 이동할 수 있는 셀이 된다.
                targets.RemoveAll(number => obj.viruses.Exists(cell => cell == number));
            });
            
            // 이동할 수 있는 셀을 리턴한다.
            return targets;
        }









    }
}
