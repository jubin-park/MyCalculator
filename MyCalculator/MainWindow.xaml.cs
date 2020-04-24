using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MyCalculator.Classes;

namespace MyCalculator
{
	public enum EMode
	{
		NUMBER,
		FUNCTION,
		OPERATOR,
		SUCCESS,
		LOAD,
		ERROR,
	}

	public class CalculatedHistory
	{
		public string Equation { get; set; }
		public string Result { get; set; }

		public CalculatedHistory(string equation, string result)
		{
			Equation = equation;
			Result = result;
		}
	}

	public class Operand
	{
		public double DomainValue;
		public double FinalValue;
		public List<EFunction> Functions = null;

		private static readonly string[] sFormats = { string.Empty, "√({0})", "sqr({0})", "1/({0})", "-({0})", "({0})/100" };

		public Operand(double initValue)
		{
			DomainValue = initValue;
			FinalValue = initValue;
			Functions = new List<EFunction>();
		}

		public override string ToString()
		{
			if (Functions.Count == 0)
			{
				return FinalValue.ToString();
			}
			string str = DomainValue.ToString();
			foreach (var type in Functions)
			{
				str = string.Format(sFormats[(int)type], str);
			}
			return str;
		}
	}

	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;
		
		public static readonly string[] ERROR_MESSAGES = { string.Empty, "0으로 나눌 수 없습니다", "정의된 수가 아닙니다", "오버플로우", "언더플로우", "알 수 없는 오류"};

		private static readonly string sCharZero = "0";
		private static readonly string sCharRoot = "√";
		private static readonly string sCharSquare = "sqr";
		private static readonly string sCharInverse = "1/";
		private static readonly string sFormatSquareRoot = sCharRoot + "({0})";
		private static readonly string sFormatSquare = sCharSquare + "({0})";
		private static readonly string sFormatInverse = sCharInverse + "({0})";
		private static readonly string[] sOperatorSigns = { string.Empty, "+", "-", "×", "÷", "=" };

		private double mLastValue = 0.0;
		private ObservableCollection<string> mEquations = new ObservableCollection<string>();
		private EMode mMode = EMode.NUMBER;
		private EOperator mOperator = EOperator.NONE;
		private Operand mOperand = new Operand(0);

		#region Public Properties
		private ObservableCollection<CalculatedHistory> mHistoryEntries = new ObservableCollection<CalculatedHistory>();
		public ObservableCollection<CalculatedHistory> HistoryEntries { get => mHistoryEntries; }

		private string mDisplayedValue = sCharZero;
		public string DisplayedValue
		{
			get
			{
				if (mDisplayedValue == null || mDisplayedValue == string.Empty)
				{
					return sCharZero;
				}
				return mDisplayedValue;
			}
			set
			{
				mDisplayedValue = value;
				notifyPropertyChanged("DisplayedValue");
			}
		}

		public string DisplayedEquations
		{
			get
			{
				StringBuilder sb = new StringBuilder();
				foreach (var x in mEquations)
				{
					sb.Append(x);
					sb.Append(" ");
				}
				if (sb.Length > 0)
				{
					--sb.Length;
				}
				return sb.ToString();
			}
		}
		#endregion

		public MainWindow()
		{
			InitializeComponent();
			DataContext = this;
			mEquations.CollectionChanged += equations_CollectionChanged;
			mHistoryEntries.CollectionChanged += historyEntries_CollectionChanged;
		}

		#region xControl Events
		private void xLabelDisplay_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			Clipboard.SetText(((Label)sender).Content.ToString());
			MessageBox.Show("Copied!", Title, MessageBoxButton.OK, MessageBoxImage.Information);
		}

		private void xButtonNumber_Click(object sender, RoutedEventArgs e)
		{
			if (mMode == EMode.OPERATOR)
			{
				DisplayedValue = sCharZero;
				mOperand = new Operand(0);
			}
			else if (mMode == EMode.FUNCTION)
			{
				mOperand = new Operand(0);
			}
			else if (mMode == EMode.SUCCESS || mMode == EMode.LOAD)
			{
				DisplayedValue = sCharZero;
				mOperand = new Operand(0);
				mEquations.Clear();
			}
			string tempStringValue = (DisplayedValue == sCharZero ? string.Empty : DisplayedValue);
			int number = getNumberOfButton(sender);
			Debug.Assert(number != -1);
			// Concatenate number behind
			tempStringValue += number.ToString();
			double parsedDouble = 0;
			if (double.TryParse(tempStringValue, out parsedDouble))
			{
				mOperand.DomainValue = parsedDouble;
				mOperand.FinalValue = mOperand.DomainValue;
				DisplayedValue = tempStringValue;
			}
			else
			{
				DisplayedValue = ERROR_MESSAGES[(int)EValueError.UNKNOWN];
				setButtonsEnabled(false);
			}
			mMode = EMode.NUMBER;
		}

		private void xButtonPoint_Click(object sender, RoutedEventArgs e)
		{
			if (mMode == EMode.OPERATOR)
			{
				mEquations.Add(sOperatorSigns[(int)mOperator]);
				mOperand = new Operand(0);
			}
			else if (mMode == EMode.FUNCTION)
			{
				mOperand = new Operand(0);
			}
			else if (mMode == EMode.SUCCESS || mMode == EMode.LOAD)
			{
				mOperand = new Operand(0);
				mEquations.Clear();
			}
			DisplayedValue = mOperand.DomainValue.ToString();
			if (!DisplayedValue.Contains("."))
			{
				DisplayedValue += ".";
			}
			mMode = EMode.NUMBER;
		}

		private void xButtonFunction_Click(object sender, RoutedEventArgs e)
		{
			if (mMode == EMode.OPERATOR)
			{
				mOperand = new Operand(0);
			}
			else if (mMode == EMode.FUNCTION)
			{
				if (mEquations.Count > 0 && !isOperatorSign(mEquations[mEquations.Count - 1]))
				{
					mEquations.RemoveAt(mEquations.Count - 1);
				}
			}
			else if (mMode == EMode.SUCCESS)
			{
				mOperand = new Operand(mLastValue);
				mEquations.Clear();
			}
			else if (mMode == EMode.LOAD)
			{
				mOperand = new Operand(mLastValue);
				mEquations.Clear();
			}
			EValueError err = EValueError.NO_ERROR;
			EFunction functionType = getFunctionOfButton(sender);
			if (functionType == EFunction.SQUARE_ROOT)
			{
				mOperand.FinalValue = SimpleMath.GetSquareRoot(mOperand.FinalValue, out err);
				mOperand.Functions.Add(EFunction.SQUARE_ROOT);
				mEquations.Add(mOperand.ToString());
			}
			else if (functionType == EFunction.SQUARE)
			{
				mOperand.FinalValue = SimpleMath.GetSquare(mOperand.FinalValue, out err);
				mOperand.Functions.Add(EFunction.SQUARE);
				mEquations.Add(mOperand.ToString());
			}
			else if (functionType == EFunction.INVERSE)
			{
				mOperand.FinalValue = SimpleMath.GetInverse(mOperand.FinalValue, out err);
				mOperand.Functions.Add(EFunction.INVERSE);
				mEquations.Add(mOperand.ToString());
			}
			else if (functionType == EFunction.NEGATE)
			{
				if (mOperand.Functions.Count == 0)
				{
					mOperand.DomainValue *= -1;
					mOperand.FinalValue = mOperand.DomainValue;
				}
				else
				{
					mOperand.FinalValue *= -1;
					mOperand.Functions.Add(EFunction.NEGATE);
					mEquations.Add(mOperand.ToString());
				}
			}
			else if (functionType == EFunction.PERCENT)
			{
				if (mOperand.Functions.Count == 0)
				{
					mOperand.DomainValue /= 100;
					mOperand.FinalValue = mOperand.DomainValue;
				}
				else
				{
					mOperand.FinalValue /= 100;
					mOperand.Functions.Add(EFunction.PERCENT);
					mEquations.Add(mOperand.ToString());
				}
			}
			// Check error
			if (err == EValueError.NO_ERROR)
			{
				DisplayedValue = mOperand.ToString();
			}
			else
			{
				DisplayedValue = ERROR_MESSAGES[(int)err];
				setButtonsEnabled(false);
			}
			mMode = EMode.FUNCTION;
		}

		private void xButtonOperate_Click(object sender, RoutedEventArgs e)
		{
			if (mMode == EMode.NUMBER || mMode == EMode.FUNCTION)
			{
				if (mOperand.Functions.Count == 0)
				{
					mEquations.Add(mOperand.ToString());
				}
				EValueError err = EValueError.NO_ERROR;
				// 이전 마지막 연산
				if (mOperator == EOperator.NONE)
				{
					mLastValue = mOperand.FinalValue;
				}
				else if (mOperator == EOperator.ADDITION)
				{
					mLastValue = SimpleMath.Add(mLastValue, mOperand.FinalValue, out err);
				}
				else if (mOperator == EOperator.SUBTRACTION)
				{
					mLastValue = SimpleMath.Subtract(mLastValue, mOperand.FinalValue, out err);
				}
				else if (mOperator == EOperator.MULTIPLICATION)
				{
					mLastValue = SimpleMath.Multiply(mLastValue, mOperand.FinalValue, out err);
				}
				else if (mOperator == EOperator.DIVISION)
				{
					mLastValue = SimpleMath.Divide(mLastValue, mOperand.FinalValue, out err);
				}
				else if (mOperator == EOperator.EQUAL)
				{
					mLastValue = mOperand.FinalValue;
				}
				// Check error
				if (err == EValueError.NO_ERROR)
				{
					DisplayedValue = mOperand.ToString();
				}
				else
				{
					DisplayedValue = ERROR_MESSAGES[(int)err];
					setButtonsEnabled(false);
					return;
				}
			}
			else if (mMode == EMode.OPERATOR)
			{
				mEquations.RemoveAt(mEquations.Count - 1);
			}
			else if (mMode == EMode.SUCCESS)
			{
				mOperand = new Operand(mLastValue);
				mEquations.Clear();
				mEquations.Add(mOperand.ToString());
			}
			else if (mMode == EMode.LOAD)
			{
				mEquations.RemoveAt(mEquations.Count - 1);
			}
			mOperator = getOperatorOfButton(sender);
			mEquations.Add(sOperatorSigns[(int)mOperator]);
			mMode = EMode.OPERATOR;
			if (mOperator == EOperator.EQUAL)
			{
				mMode = EMode.SUCCESS;
				mOperand = new Operand(mLastValue);
				mHistoryEntries.Add(new CalculatedHistory(DisplayedEquations, mLastValue.ToString()));
			}
		}

		private void xButtonAllClear_Click(object sender, RoutedEventArgs e)
		{
			mMode = EMode.NUMBER;
			mOperator = EOperator.NONE;
			mOperand = new Operand(0);
			mLastValue = 0;
			mEquations.Clear();
			DisplayedValue = sCharZero;
			setButtonsEnabled(true);
		}

		private void xButtonDelete_Click(object sender, RoutedEventArgs e)
		{
			if (mMode == EMode.SUCCESS)
			{
				mEquations.Clear();
			}
			else
			{
				DisplayedValue = DisplayedValue.Substring(0, DisplayedValue.Length - 1);
				double parsedDouble = 0;
				if (double.TryParse(DisplayedValue, out parsedDouble))
				{
					mOperand.DomainValue = parsedDouble;
					mOperand.FinalValue = mOperand.DomainValue;
				}
				else
				{
					DisplayedValue = ERROR_MESSAGES[(int)EValueError.UNKNOWN];
					setButtonsEnabled(false);
				}
			}
		}

		private void xListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var item = (sender as ListView).SelectedItem;
			if (item != null)
			{
				var history = item as CalculatedHistory;
				double result = double.Parse(history.Result);
				string[] equations = history.Equation.Split(' ');
				mOperand = new Operand(result);
				mEquations.Clear();
				foreach (var chunk in equations)
				{
					mEquations.Add(chunk);
				}
				mMode = EMode.LOAD;
				mOperator = EOperator.EQUAL;
				mOperand = new Operand(result);
				mLastValue = result;
				DisplayedValue = mLastValue.ToString();
			}
		}

		private void xButtonClearHistory_Click(object sender, RoutedEventArgs e)
		{
			mHistoryEntries.Clear();
		}

		private void equations_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			notifyPropertyChanged("DisplayedEquations");
		}

		private void historyEntries_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			notifyPropertyChanged("HistoryEntries");
		}
		#endregion

		private void notifyPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		private bool isOperatorSign(string str)
		{
			for (int i = 1; i < sOperatorSigns.Length; ++i)
			{
				if (str == sOperatorSigns[i])
				{
					return true;
				}
			}
			return false;
		}

		private EFunction getFunctionOfButton(object sender)
		{
			if (sender == xButtonSquareRoot)
			{
				return EFunction.SQUARE_ROOT;
			}
			else if (sender == xButtonSquare)
			{
				return EFunction.SQUARE;
			}
			else if (sender == xButtonInverse)
			{
				return EFunction.INVERSE;
			}
			else if (sender == xButtonSign)
			{
				return EFunction.NEGATE;
			}
			else if (sender == xButtonPercent)
			{
				return EFunction.PERCENT;
			}
			return EFunction.NONE;
		}

		private EOperator getOperatorOfButton(object sender)
		{
			if (sender == xButtonAdd)
			{
				return EOperator.ADDITION;
			}
			else if (sender == xButtonSubtract)
			{
				return EOperator.SUBTRACTION;
			}
			else if (sender == xButtonMutiply)
			{
				return EOperator.MULTIPLICATION;
			}
			else if (sender == xButtonDivide)
			{
				return EOperator.DIVISION;
			}
			else if (sender == xButtonEqual)
			{
				return EOperator.EQUAL;
			}
			return EOperator.NONE;
		}

		private int getNumberOfButton(object sender)
		{
			if (sender == xButton0)
			{
				return 0;
			}
			else if (sender == xButton1)
			{
				return 1;
			}
			else if (sender == xButton2)
			{
				return 2;
			}
			else if (sender == xButton3)
			{
				return 3;
			}
			else if (sender == xButton4)
			{
				return 4;
			}
			else if (sender == xButton5)
			{
				return 5;
			}
			else if (sender == xButton6)
			{
				return 6;
			}
			else if (sender == xButton7)
			{
				return 7;
			}
			else if (sender == xButton8)
			{
				return 8;
			}
			else if (sender == xButton9)
			{
				return 9;
			}
			return -1;
		}

		private void setButtonsEnabled(bool value)
		{
			xButtonSquareRoot.IsEnabled = value;
			xButtonSquare.IsEnabled = value;
			xButtonInverse.IsEnabled = value;
			xButtonSign.IsEnabled = value;
			xButtonPoint.IsEnabled = value;
			xButtonPercent.IsEnabled = value;
			xButtonEqual.IsEnabled = value;
			xButtonDivide.IsEnabled = value;
			xButtonMutiply.IsEnabled = value;
			xButtonSubtract.IsEnabled = value;
			xButtonAdd.IsEnabled = value;
			xButtonDelete.IsEnabled = value;
			xButton0.IsEnabled = value;
			xButton1.IsEnabled = value;
			xButton2.IsEnabled = value;
			xButton3.IsEnabled = value;
			xButton4.IsEnabled = value;
			xButton5.IsEnabled = value;
			xButton6.IsEnabled = value;
			xButton7.IsEnabled = value;
			xButton8.IsEnabled = value;
			xButton9.IsEnabled = value;
			double opacity = (value ? 1.0 : 0.2);
			xButtonSquareRoot.Opacity = opacity;
			xButtonSquare.Opacity = opacity;
			xButtonInverse.Opacity = opacity;
			xButtonSign.Opacity = opacity;
			xButtonPoint.Opacity = opacity;
			xButtonPercent.Opacity = opacity;
			xButtonEqual.Opacity = opacity;
			xButtonDivide.Opacity = opacity;
			xButtonMutiply.Opacity = opacity;
			xButtonSubtract.Opacity = opacity;
			xButtonAdd.Opacity = opacity;
			xButtonDelete.Opacity = opacity;
			xButton0.Opacity = opacity;
			xButton1.Opacity = opacity;
			xButton2.Opacity = opacity;
			xButton3.Opacity = opacity;
			xButton4.Opacity = opacity;
			xButton5.Opacity = opacity;
			xButton6.Opacity = opacity;
			xButton7.Opacity = opacity;
			xButton8.Opacity = opacity;
			xButton9.Opacity = opacity;
		}
	}
}
