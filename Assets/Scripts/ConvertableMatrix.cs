using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = System.Object;

// added because I wanted to encapsulate my functionality and get access to a string version of the matrix
[Serializable] public class ConvertableMatrix<T> : IEnumerable<T> where T : IConvertable {

	// internal data collection
	private readonly T[,] _matrix; 
	
	// constructor to set initial size
	public ConvertableMatrix(int sizeX, int sizeY) => _matrix = new T[sizeX, sizeY]; 
	
	// don't actually know what this is called but to get access to the internal matrix directly 
	public T this[int x, int y] { 
		get => _matrix[x, y];
		set => _matrix[x, y] = value;
	}

	// didn't end up using, breaking YAGNI pricniple
	public void Clear() { 
		for (int y = 0; y < _matrix.GetLength(1); y++) 
			for (int x = 0; x < _matrix.GetLength(0); x++) 
				_matrix[x, y] = default;
	}

	public int Length => _matrix.Length;
	
	// flattens the matrix a single string array using 
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

	public IEnumerator<T> GetEnumerator() => _matrix.Cast<T>().GetEnumerator(); // in the IEnumarable interface
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator(); // in the IEnumarable interface
}


// interface so that It can be converted to a string array
public interface IConvertable {
	// this is supposed to return a override of ToString or a any other string. 
	public string Convert(); 

}