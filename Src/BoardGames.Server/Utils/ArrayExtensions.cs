namespace BoardGames.Server.Utils;

public static class ArrayExtensions
{
	public static T[][] ToArrayOfArrays<T>( this T[,] arr2d )
	{
		var height = arr2d.GetLength( 0 );
		var width = arr2d.GetLength( 1 );
		var arr = new T[height][];
		for ( int row = 0; row < height; row++ )
		{
			arr[row] = new T[width];
			for ( int col = 0; col < width; col++ )
			{
				arr[row][col] = arr2d[row, col];
			}
		}

		return arr;
	}

	public static T[,] To2DArray<T>( this T[][] arrOfArr )
	{
		var height = arrOfArr.Length;
		var width = arrOfArr[0].Length;
		var arr2d = new T[height, width];

		for ( int row = 0; row < height; row++ )
		{
			if ( arrOfArr[row].Length != width )
			{
				throw new ArgumentException( "All rows of 2d array must heve the same length.", nameof( arrOfArr ) );
			}

			for ( int col = 0; col < width; col++ )
			{
				arr2d[row, col] = arrOfArr[row][col];
			}
		}

		return arr2d;
	}

}
