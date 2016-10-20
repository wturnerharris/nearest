using Android.Animation;
using Android.Content;
using Android.Graphics;
using PaintStyle = Android.Graphics.Paint.Style;
using Android.Util;
using Android.Views;
using Android.Widget;
using Android.Views.Animations;

namespace Nearest.Droid
{
	public class CircleView : Button
	{
		Paint circlePaint;
		RectF circleRect;

		// Attrs
		int circleRadius;
		Color circleFillColor = Color.Argb(50, 0, 0, 0);
		const float circleColorDarkness = 0.99f;
		float circleEndAngle;
		float circleStartAngle = 90;

		public CircleView(Context context, IAttributeSet attrs) : base(context, attrs)
		{
			Initialize(context, attrs);
		}

		public CircleView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
		{
			Initialize(context, attrs);
		}

		public float CircleAngle
		{
			get { return circleEndAngle; }
			set
			{
				circleEndAngle = value;
				Invalidate();
			}
		}

		public Color BackgroundColor
		{
			get { return circleFillColor; }
			set
			{
				circleFillColor = value;
				Invalidate();
			}
		}

		public void EnterReveal()
		{
			// make the view visible and start the animation
			Visibility = ViewStates.Visible;

			var cAnimatorSet = new AnimatorSet();
			var objScaleX = ObjectAnimator.OfFloat(this, "scaleX", 1, 0.9f, 0.9f, 1.2f, 1.2f, 1.1f, 1.1f, 1.1f, 1.1f, 1);
			var objScaleY = ObjectAnimator.OfFloat(this, "scaleY", 1, 0.9f, 0.9f, 1.2f, 1.2f, 1.1f, 1.1f, 1.1f, 1.1f, 1);
			var objRotate = ObjectAnimator.OfFloat(this, "rotation", 0, -4, -4, 3, -3, 3, -3, 3, -3, 0);

			cAnimatorSet.PlayTogether(objScaleX, objScaleY, objRotate);
			cAnimatorSet.Start();
		}

		public void Hide()
		{
			Visibility = ViewStates.Invisible;
		}

		//Returns darker version of specified <code>color</code>.
		public Color DarkerColor(float factor)
		{
			var color = circleFillColor;
			//int a = Color.GetAlphaComponent(color);
			int a = (int)(255 * 0.25);
			int r = Color.GetRedComponent(color);
			int g = Color.GetGreenComponent(color);
			int b = Color.GetBlueComponent(color);

			return Color.Argb(
				a,
				System.Math.Max((int)(r * factor), 0),
				System.Math.Max((int)(g * factor), 0),
				System.Math.Max((int)(b * factor), 0)
			);
		}

		protected override void OnDraw(Canvas canvas)
		{
			base.OnDraw(canvas);

			//circlePaint.Color = Color.ParseColor("#FFEE352E");
			circlePaint.Color = DarkerColor(0.9f);
			canvas.DrawArc(circleRect, -circleStartAngle, circleEndAngle, true, circlePaint);
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
				{
					// Check height, if height is smaller than
					// width, then go half height as radius.
					circleRadius = tempRadiusHeight;
				}
			}
			int circleDiameter = circleRadius * 2;
			circleRect = new RectF(0, 0, circleDiameter, circleDiameter);

			int measuredHeight = MeasureHeight(heightMeasureSpec);
			SetMeasuredDimension(measuredWidth, measuredHeight);
		}

		int MeasureHeight(int measureSpec)
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

		int MeasureWidth(int measureSpec)
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

		void Initialize(Context context, IAttributeSet attrs)
		{
			// Go through all custom attrs.
			//var attrsArray = context.ObtainStyledAttributes(attrs, Resource.Styleable.circle_view);
			//circleFillColor = attrsArray.GetColor(Resource.Styleable.circle_view_cFillColor, 16777215);

			circlePaint = new Paint(PaintFlags.AntiAlias);
			circlePaint.SetStyle(PaintStyle.Fill);
			circlePaint.Color = DarkerColor(0.2f);

			// Google tells us to call recycle.
			//attrsArray.Recycle();
		}

		public class CircleAngleAnimation : Animation
		{

			CircleView circle;

			float oldAngle;
			float newAngle;

			public CircleAngleAnimation(CircleView newCircle, int newAngleValue)
			{
				this.oldAngle = newCircle.CircleAngle;
				this.newAngle = newAngleValue;
				this.circle = newCircle;
			}

			protected override void ApplyTransformation(float interpolatedTime, Transformation t)
			{
				float angle = oldAngle + ((newAngle - oldAngle) * interpolatedTime);

				circle.CircleAngle = angle;
				circle.RequestLayout();
			}
		}
	}
}
