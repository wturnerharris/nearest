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
		Paint paint;
		RectF rect;

		// Attrs
		int radius;
		Color fillColor = Color.Argb(50, 0, 0, 0);
		const float darkness = 0.99f;
		float endAngle;
		float START_ANGLE = 90;

		public CircleView(Context context, IAttributeSet attrs) : base(context, attrs)
		{
			Initialize(context, attrs);
		}

		public CircleView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
		{
			Initialize(context, attrs);
		}

		public float EndAngle
		{
			get { return endAngle; }
			set
			{
				endAngle = value;
				Invalidate();
			}
		}

		public Color BackgroundColor
		{
			get { return fillColor; }
			set
			{
				fillColor = value;
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
			var color = fillColor;
			//int a = Color.GetAlphaComponent(color);
			var a = (int)(255 * 0.25);
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

			//paint.Color = Color.ParseColor("#FFEE352E");
			paint.Color = DarkerColor(0.2f);
			canvas.DrawArc(rect, -START_ANGLE, endAngle, false, paint);
		}

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{

			int measuredWidth = MeasureWidth(widthMeasureSpec);
			int measuredHeight = MeasureHeight(heightMeasureSpec);
			int s = 10;

			// No radius specified.
			if (radius == 0)
			{
				// Check width size. Make radius half of available.
				radius = measuredWidth / 2;
				int tmpRadius = measuredHeight / 2;
				if (tmpRadius < radius)
				{
					// Check height, if height is smaller than
					// width, then go half height as radius.
					radius = tmpRadius;
				}
			}
			int diameter = radius * 2;
			rect = new RectF(s, s, diameter - s, diameter - s);

			SetMeasuredDimension(measuredWidth, measuredHeight);
		}

		int MeasureHeight(int measureSpec)
		{
			var specMode = MeasureSpec.GetMode(measureSpec);
			int specSize = MeasureSpec.GetSize(measureSpec);
			int result = 0;
			if (specMode == MeasureSpecMode.AtMost)
			{
				result = radius * 2;
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
			var attrsArray = context.ObtainStyledAttributes(attrs, Resource.Styleable.circle_view);
			fillColor = attrsArray.GetColor(Resource.Styleable.circle_view_cFillColor, 16777215);

			//paint = new Paint(PaintFlags.AntiAlias);
			paint = new Paint();
			paint.AntiAlias = true;
			paint.SetStyle(PaintStyle.Stroke);
			paint.StrokeWidth = 20;
			paint.Color = DarkerColor(0.2f);

			// Google tells us to call recycle.
			attrsArray.Recycle();
		}

		public class CircleAngleAnimation : Animation
		{
			readonly CircleView circle;
			readonly float oldAngle;
			readonly float newAngle;

			public CircleAngleAnimation(CircleView newCircle, int newAngleValue)
			{
				oldAngle = newCircle.EndAngle;
				newAngle = newAngleValue;
				circle = newCircle;
			}

			protected override void ApplyTransformation(float interpolatedTime, Transformation t)
			{
				float angle = oldAngle + ((newAngle - oldAngle) * interpolatedTime);

				circle.EndAngle = angle;
				circle.RequestLayout();
			}
		}
	}
}
