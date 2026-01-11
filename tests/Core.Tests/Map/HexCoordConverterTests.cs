namespace Core.Tests.Map;

using Core.Map.Algorithms;
using Core.Map.Grid;
public class HexCoordConverterTests
    {
        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 0)]
        [InlineData(1, 1)]
        [InlineData(-1, 0)]
        [InlineData(2, -3)]
        public void ToOffset_FromOffset_RoundTrips_Axial(int q, int r)
        {
            var axial = new HexCoord(q, r);

            var (col, row) = HexCoordConverter.ToOffset(axial);
            var roundTripped = HexCoordConverter.FromOffset(col, row);

            Assert.Equal(axial, roundTripped);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 0)]
        [InlineData(1, 2)]
        [InlineData(-1, 3)]
        [InlineData(4, -2)]
        public void FromOffset_ToOffset_RoundTrips_Offset(int col, int row)
        {
            var axial = HexCoordConverter.FromOffset(col, row);
            var (roundCol, roundRow) = HexCoordConverter.ToOffset(axial);

            Assert.Equal(col, roundCol);
            Assert.Equal(row, roundRow);
        }
    }

