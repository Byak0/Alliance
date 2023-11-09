using TaleWorlds.Library;

namespace Alliance.Common.Extensions.SAE.Models
{
    public class SaeMarkerWithIdAndPos
    {
        public int Id { get; }
        public MatrixFrame Pos { get; }

        public SaeMarkerWithIdAndPos(int id, MatrixFrame pos)
        {
            Id = id;
            Pos = pos;
        }
    }
}
