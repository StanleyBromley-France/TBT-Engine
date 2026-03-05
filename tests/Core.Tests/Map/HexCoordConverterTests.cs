namespace Core.Tests.Map;

using Core.Domain.Types;
using Core.Map.Search;

public class HexCoordConverterTests
    {
        [Theory]
        [InlineData(0, 0, 0, 0)]
        [InlineData(1, 0, 1, 0)]
        [InlineData(2, 0, 2, 1)]
        [InlineData(3, 0, 3, 1)]
        [InlineData(-1, 0, -1, -1)]
        [InlineData(-2, 1, -2, 0)]
        public void ToOffset_Uses_Known_OddQ_Mappings(int q, int r, int expectedCol, int expectedRow)
        {
            var axial = new HexCoord(q, r);

            var (col, row) = HexCoordConverter.ToOffset(axial);

            Assert.Equal(expectedCol, col);
            Assert.Equal(expectedRow, row);
        }

        [Theory]
        [InlineData(0, 0, 0, 0)]
        [InlineData(1, 0, 1, 0)]
        [InlineData(2, 1, 2, 0)]
        [InlineData(3, 1, 3, 0)]
        [InlineData(-1, -1, -1, 0)]
        [InlineData(-2, 0, -2, 1)]
        public void FromOffset_Uses_Known_OddQ_Mappings(int col, int row, int expectedQ, int expectedR)
        {
            var axial = HexCoordConverter.FromOffset(col, row);

            Assert.Equal(new HexCoord(expectedQ, expectedR), axial);
        }

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

