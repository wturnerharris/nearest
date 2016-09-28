
using System;

using Android.Animation;
using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;

namespace Nearest.Droid
{
	public class CircleView : View
	{

		private Paint circlePaint;
		private Paint circleStrokePaint;
		private RectF circleArc;

		// Attrs
		private int circleRadius;
		private int circleFillColor;
		private int circleStrokeColor;
		private int circleStartAngle;
		private int circleEndAngle;

		/*end circle view*/

		const int DefaultHeight = 20;
		const int DefaultWidth = 120;

		Paint _negativePaint;
		double _position = 0.5;
		Paint _positivePaint;

		public CircleView(Context context, IAttributeSet attrs) :
			base(context, attrs)
		{
			Initialize();
		}

		public CircleView(Context context, IAttributeSet attrs, int defStyle) :
			base(context, attrs, defStyle)
		{
			Initialize();
		}

		public double CircleValue
		{
			get { return _position; }
			set
			{
				_position = Math.Max(0f, Math.Min(value, 1f));
				Invalidate();
			}
		}

		public void SetCircleValue(double value, bool animate)
		{
			if (!animate)
			{
				CircleValue = value;
				return;
			}

			ValueAnimator animator = ValueAnimator.OfFloat((float)_position, (float)Math.Max(0f, Math.Min(value, 1f)));
			animator.SetDuration(500);

			animator.Update += (sender, e) => CircleValue = (double)e.Animation.AnimatedValue;
			animator.Start();
		}

		protected override void OnDraw(Canvas canvas)
		{
			base.OnDraw(canvas);
			float middle = canvas.Width * (float)_position;

			canvas.DrawPaint(_negativePaint);

			canvas.DrawRect(0, 0, middle, canvas.Height, _positivePaint);

			// Move canvas down and right 1 pixel.
			// Otherwise the stroke gets cut off.
			canvas.Translate(1, 1);
			//circlePaint.SetColorFilter(circleFillColor);
			canvas.DrawArc(circleArc, circleStartAngle, circleEndAngle, true, circlePaint);
			canvas.DrawArc(circleArc, circleStartAngle, circleEndAngle, true, circleStrokePaint);
		}

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{

			int measuredWidth = MeasureWidth(widthMeasureSpec);
			// No radius specified.
			if (circleRadius == 0)
			{
				// Check width size. Make radius half of available.
				circleRadius = measuredWidth / 2;
				int tempRadiusHeight = MeasureHeight(heightMeasureSpec) / 2;
				if (tempRadiusHeight < circleRadius)
					// Check height, if height is smaller than
					// width, then go half height as radius.
					circleRadius = tempRadiusHeight;
			}
			// Remove 2 pixels for the stroke.
			int circleDiameter = circleRadius * 2 - 2;
			// RectF(float left, float top, float right, float bottom)
			circleArc = new RectF(0, 0, circleDiameter, circleDiameter);
			int measuredHeight = MeasureHeight(heightMeasureSpec);
			SetMeasuredDimension(measuredWidth, measuredHeight);
		}

		private int MeasureHeight(int measureSpec)
		{
			var specMode = MeasureSpec.GetMode(measureSpec);
			int specSize = MeasureSpec.GetSize(measureSpec);
			int result = 0;
			if (specMode == MeasureSpecMode.AtMost)
			{
				result = circleRadius * 2;
			}
			else if (specMode == MeasureSpecMode.Exactly)
			{
				result = specSize;
			}
			return result;
		}

		private int MeasureWidth(int measureSpec)
		{
			var specMode = MeasureSpec.GetMode(measureSpec);
			int specSize = MeasureSpec.GetSize(measureSpec);
			int result = 0;
			if (specMode == MeasureSpecMode.AtMost)
			{
				result = specSize;
			}
			else if (specMode == MeasureSpecMode.Exactly)
			{
				result = specSize;
			}
			return result;
		}

		void Initialize()
		{
			_positivePaint = new Paint
			{
				AntiAlias = true,
				Color = Color.Rgb(0x99, 0xcc, 0)
			};
			_positivePaint.SetStyle(Paint.Style.FillAndStroke);

			_negativePaint = new Paint
			{
				AntiAlias = true,
				Color = Color.Rgb(0xff, 0x44, 0x44)
			};
			_negativePaint.SetStyle(Paint.Style.FillAndStroke);
		}

		public void init(AttributeSet attrs)
		{
			// Go through all custom attrs.
			TypedArray attrsArray = Android.Content.Context.ObtainStyledAttributes(attrs, Resource.Styleable.circleview);
			circleRadius = attrsArray.getInteger(R.styleable.circleview_cRadius, 0);
			circleFillColor = attrsArray.getColor(R.styleable.circleview_cFillColor, 16777215);
			circleStrokeColor = attrsArray.getColor(R.styleable.circleview_cStrokeColor, -1);
			circleStartAngle = attrsArray.getInteger(R.styleable.circleview_cAngleStart, 0);
			circleEndAngle = attrsArray.getInteger(R.styleable.circleview_cAngleEnd, 360);
			// Google tells us to call recycle.
			attrsArray.recycle();
		}
	}
}
