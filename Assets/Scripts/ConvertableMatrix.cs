using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = System.Object;


[Serializable] public class ConvertableMatrix<T> : IEnumerable<T> where T : IConvertable {

	private readonly T[,] _matrix;

	public ConvertableMatrix(int sizeX, int sizeY) => _matrix = new T[sizeX, sizeY];

	public T this[int x, int y] {
		get => _matrix[x, y];
		set => _matrix[x, y] = value;
	}

	public int Length => _matrix.Length;

	public string[] GetStringBoard {
		get {
			string[] intArray = new string[_matrix.Length];

			int i = 0;
			foreach (var item in _matrix) {
				intArray[i] = item.Convert();
				i++;
			}

			return intArray;
		}
	}

	public IEnumerator<T> GetEnumerator() => _matrix.Cast<T>().GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	

}

public interface IConvertable {
	public string Convert();

}