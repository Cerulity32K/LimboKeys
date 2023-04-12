using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Collections;
using MonoGame.Extended.Input;
using Newtonsoft.Json;

new LimboKeys().Run();
public class LimboKey
{
	public int OldIdx;
	public int NewIdx;
	public LimboKey(int oldi, int newi)
	{
		OldIdx = oldi;
		NewIdx = newi;
	}
	public LimboKey(int i)
	{
		OldIdx = i;
		NewIdx = i;
	}
	public void UpdatePos(int newi)
	{
		OldIdx = NewIdx;
		NewIdx = newi;
	}
}
public static class Extensions
{
	public static T Choose<T>(this Random rand, IEnumerable<T> collection) => collection.ElementAt(rand.Next(collection.Count()));
	public static Color Lerp(this Color a, Color b, float c)
	{
		return new Color(
			a.R + (b.R - a.R) * c,
			a.G + (b.G - a.G) * c,
			a.B + (b.B - a.B) * c,
			a.A + (b.A - a.A) * c
		);
	}

	public static Color Lerp(this Color a, Color b, Color c)
	{
		return new Color(
			a.R + (b.R - a.R) * c.R,
			a.G + (b.G - a.G) * c.G,
			a.B + (b.B - a.B) * c.B,
			a.A + (b.A - a.A) * c.A
		);
	}

	public static Color Add(this Color a, Color b)
	{
		return new Color(
			a.R + b.R,
			a.G + b.G,
			a.B + b.B,
			a.A + b.A
		);
	}
}
public enum EasingType
{
	linear,
	sine,
	sineIn,
	sineOut,
	backOut
}
[Serializable]
public class Config
{
	public float spinSpeed;
	public float rampSpeed;
	public float shakeStrength;

	public float circleWidth;
	public float circleHeight;
	public float xSpacing;
	public float ySpacing;

	public float keyRadius;
	public float winScale;
	public float textScale;

	public bool debug;

	public EasingType easing;
}
public class LimboKeys : Game
{
	GraphicsDeviceManager Graphics;
	SpriteBatch Batch;
	SoundEffect Isolation;
	SpriteFont JBMono;
	Config Cfg;
	public Vector2 WinCenter;
	float LoopTime = 0;
	int Loops = 0;
	int ChosenKey = 0;
	float FlashTime = 0;
	bool HasChosen = false;
	public static Random Random = new Random();
	int Correct = Random.Next(8);
	float ChosenTime = 0;
	public Func<float, float> KeyEasing;
	public static List<LimboKey> Keys = new List<LimboKey>()
	{
		new LimboKey(0), new LimboKey(1),
		new LimboKey(2), new LimboKey(3),
		new LimboKey(4), new LimboKey(5),
		new LimboKey(6), new LimboKey(7),
	};
	public static List<Func<int[]>> Shuffles;
	public static List<Func<int[]>> SpinShuffles;
	public static List<Vector2> KeyPositions = new List<Vector2>();
	public static List<string> Colors = new List<string>()
    {
		"Lime",
		"Orange",
		"Red",
		"Magenta",
		"Indigo",
		"Blue",
		"Cyan",
		"Green",
    };
	public LimboKeys()
	{
		IsMouseVisible = true;
		Graphics = new GraphicsDeviceManager(this);
		Cfg = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));

		Cfg.shakeStrength *= Cfg.winScale;
		Cfg.circleWidth *= Cfg.winScale;
		Cfg.circleHeight *= Cfg.winScale;
		Cfg.xSpacing *= Cfg.winScale;
		Cfg.ySpacing *= Cfg.winScale;
		Cfg.textScale *= Cfg.winScale;
		Cfg.keyRadius *= Cfg.winScale;

		WinCenter = new Vector2(800, 450) * Cfg.winScale;
		if (Cfg is null) { Console.WriteLine("Settings invalid!"); Exit(); }
		Graphics.PreferredBackBufferWidth = (int)(1600 * Cfg.winScale);
		Graphics.PreferredBackBufferHeight = (int)(900 * Cfg.winScale);
		Graphics.ApplyChanges();

		SpinShuffles = new List<Func<int[]>>()
		{
			() =>
            {
				int[] arr = new int[8];
				int start = Random.Next(2, 6);
				for (int i = start; i < arr.Length; i++)
				{
					arr[i] = i - start;
				}
				for (int i = 0; i < start; i++)
				{
					arr[i] = i + (8 - start);
				}
				Console.WriteLine($"[{string.Join(',',arr.Select(i=>i.ToString()))}]");
				return arr;
            }
		};

		Shuffles = new List<Func<int[]>>()
		{
			() => new int[] {
				1, 2,
				3, 4,
				5, 6,
				7, 0
			},

			() => Loops >= 24 ? Shuffles[Random.Next(2, Shuffles.Count)]() : new int[] {
				7, 0,
				1, 2,
				3, 4,
				5, 6
			},

			() => new int[] {
				2, 0,
				4, 1,
				6, 3,
				7, 5
			},

			() => new int[]
			{
				3, 2,
				1, 0,
				7, 6,
				5, 4
			},

			() => new int[] {
				1, 0,
				4, 5,
				2, 3,
				7, 6
			},

			() => new int[] {
				2, 3,
				4, 5,
				6, 7,
				1, 0
			},

			/*() => new int[]
			{
				6, 4,
				7, 5,
				2, 0,
				3, 1
			},
			/*() => new int[] {
				0, 1,
				2, 3,
				4, 5,
				6, 7
			}.Shuffle(Random).ToArray(),*/
		};

		switch (Cfg.easing)
        {
            case EasingType.linear: KeyEasing = NoEase; break;
            case EasingType.sine: KeyEasing = SinEase; break;
            case EasingType.sineIn: KeyEasing = SinInEase; break;
            case EasingType.sineOut: KeyEasing = SinOutEase; break;
            case EasingType.backOut: KeyEasing = BackOutEase; break;
            default: break;
        }
	}
	protected override void Initialize()
	{
		for (int i = 0; i < 8; i++)
		{
			float x = (i % 2) * Cfg.xSpacing - Cfg.xSpacing / 2;
			float y = (i / 2) * Cfg.ySpacing - Cfg.ySpacing * 1.5f;
			KeyPositions.Add(WinCenter + new Vector2(x, y));
		}
		Batch = new SpriteBatch(GraphicsDevice);
		base.Initialize();
	}
    protected override void LoadContent()
    {
		JBMono = Content.Load<SpriteFont>("JetBrains Mono");
		Isolation = Content.Load<SoundEffect>("limbo keys");
		Isolation.Play();
        base.LoadContent();
    }
    public static Color Rainbow(float t) => new Color(
		MathF.Sin(t) / 2 + 0.5f,
		MathF.Sin(t + MathF.PI / 3 * 2) / 2 + 0.5f,
		MathF.Sin(t + MathF.PI / 3 * 4) / 2 + 0.5f, 1.0f
	);
	public static float SinEase(float t) => (MathF.Cos(t * MathF.PI) - 1) / -2;
	public static float SinEase01(float t) => SinEase(MathHelper.Clamp(t, 0, 1));
	public static float SinInEase(float t) => -MathF.Cos(MathF.PI * t / 2) + 1;
	public static float SinOutEase(float t) => MathF.Sin(MathF.PI * t / 2);
	public static float BackOutEase(float t) => MathF.Sin(MathF.PI * t) / 2 + t;
	public static float SinPeriod01(float t) => -MathF.Cos(MathF.Tau * t) / 2 + 0.5f;
	public static float NoEase(float t) => t;
	protected override void Draw(GameTime gameTime)
	{
		GraphicsDevice.Clear(Color.Black);
		float dt = gameTime.GetElapsedSeconds();
		LoopTime += dt * (Loops >= 24 ? Cfg.rampSpeed * Loops + 200f - 24 * Cfg.rampSpeed : 200f) / 60f;
		float total = (float)gameTime.TotalGameTime.TotalSeconds;
		float beats = total * (200f / 60f);
		if (LoopTime >= 1)
		{
			LoopTime--;
			Loops++;
			if (beats >= 8)
			{
				if (beats <= 40)
				{
					int[] shuf = Random.Choose(/*beats >= 24 ? SpinShuffles : */Shuffles)();
					ChosenKey = shuf[ChosenKey];
					foreach (LimboKey key in Keys)
					{
						key.UpdatePos(shuf[key.NewIdx]);
					}
				}
				else
				{
					foreach (LimboKey key in Keys)
					{
						key.OldIdx = key.NewIdx;
					}
				}
			}
		} 
		Batch.Begin();
		{
			MouseStateExtended mse = MouseExtended.GetState();
			if (HasChosen)
            {
				if (FlashTime > 0)
				{
					GraphicsDevice.Clear(Rainbow(Keys[ChosenKey].NewIdx * MathF.Tau / 8) * FlashTime);
					FlashTime -= dt;
				}
				ChosenTime += dt;
				if (ChosenTime > 2)
                {
					GraphicsDevice.Clear(Color.White * (-(ChosenTime - 2) / 2 + 1));
					if (ChosenTime > 5)
                    {
						bool success = Correct == ChosenKey;
						Color consensusColor = success ? Color.Green : Color.Red;
						string consensusMsg = success ? "Success" : "Try again";
						Vector2 consensusCenter = JBMono.MeasureString(consensusMsg) / 2.5f * Cfg.textScale;
						GraphicsDevice.Clear(consensusColor * (-(ChosenTime - 5) / 4 + 1));

						Batch.DrawString(
							JBMono,
							consensusMsg,
							WinCenter - consensusCenter,
							consensusColor,
							0, Vector2.Zero,
							0.8f * Cfg.textScale,
							SpriteEffects.None, 0
						);
						if (!success)
						{
							string corMsg = $"You chose: {Colors[Keys[ChosenKey].NewIdx]}. Correct: {Colors[Keys[Correct].NewIdx]}.";
							Batch.DrawString(
								JBMono,
								corMsg,
								new Vector2(800, 650) * Cfg.winScale - JBMono.MeasureString(corMsg) / 20 * Cfg.textScale,
								Color.Red,
								0, Vector2.Zero,
								0.1f * Cfg.textScale,
								SpriteEffects.None, 0
							);
						}

						if (ChosenTime > 7.5f)
						{
							string plugMsg = "Made by Cerulity32K: github.com/Cerulity64X";
							Vector2 plugCenter = JBMono.MeasureString(plugMsg) / 20 * Cfg.textScale;
							Batch.DrawString(
								JBMono,
								plugMsg,
								new Vector2(800, 850) * Cfg.winScale - plugCenter,
								Color.LightPink * SinEase01(MathHelper.Clamp(ChosenTime - 7.5f, 0, 2) / 2),
								0, Vector2.Zero,
								0.1f * Cfg.textScale,
								SpriteEffects.None, 0
							);
						}
					}
					string titleMsg = ChosenTime < 2.15f ? "Not Limbo Keys" : "Purgatory Keys";
					Vector2 titleCenter = JBMono.MeasureString(titleMsg) / 20 * Cfg.textScale;
					float titleLerpFac = SinEase01((ChosenTime - 3f) / 2);
					Batch.DrawString(
						JBMono,
						titleMsg,
						Vector2.Lerp(WinCenter - titleCenter * 5, new Vector2(800, 50) * Cfg.textScale - titleCenter, titleLerpFac),
						Color.White,
						0, Vector2.Zero,
						MathHelper.Lerp(0.5f, 0.1f, titleLerpFac) * Cfg.textScale,
						SpriteEffects.None, 0
					);
                }
            }
            else
            {
				List<Vector2> kpos = KeyPositions.Select((kpos, idx) => {
					if (beats < 24)
					{
						return kpos;
					}
					else
					{
						float circPer = 3f;
						Func<float, float> bm20qf = b => MathF.Pow(b - 24, 2) * Cfg.spinSpeed;
						float bm20q = bm20qf(beats);
						float offset = bm20qf(40);
						float bm36 = beats - 40;
						float bm36log = offset + MathF.Log2(bm36 * 100f * Cfg.spinSpeed + 2) - 1;

						if (beats < 40)
						{
							Vector2 circleOffset = new Vector2(
								MathF.Sin(bm20q + (idx * MathF.Tau / 8) + circPer),
								MathF.Cos(bm20q + (idx * MathF.Tau / 8) + circPer)
							) * new Vector2(Cfg.circleWidth, Cfg.circleHeight);
							return ((Vector2.Lerp(
								kpos/* + new Vector2(
									MathF.Sin(bm20q + idx) * bm20q,
									MathF.Cos(bm20q + idx) * bm20q
								)*/,
								WinCenter + circleOffset,
								SinEase(SinEase((beats - 24) / 16))
							) - WinCenter) * (SinPeriod01(SinEase((beats - 40) / 16)) * 0.75f + 1)) + WinCenter;
						}
						else
						{
							Vector2 circleOffset = new Vector2(
								MathF.Sin(bm36log + (idx * MathF.Tau / 8) + circPer),
								MathF.Cos(bm36log + (idx * MathF.Tau / 8) + circPer)
							) * new Vector2(Cfg.circleWidth, Cfg.circleHeight);
							return WinCenter + circleOffset;
						}
					}
				}).ToList();
				int i = 0;
				foreach (LimboKey key in Keys)
				{
					bool onRight = Correct == key.NewIdx;
					Vector2 shake;
					if (beats >= 24 && beats <= 40)
					{
						float x = Random.NextSingle(-beats + 24, beats - 24);
						float y = Random.NextSingle(-beats + 24, beats - 24);
						shake = new Vector2(x, y) / 16 * Cfg.shakeStrength;
						shake *= shake;
					}
                    else
                    {
						shake = Vector2.Zero;
                    }
					CircleF circ = new CircleF(
						Vector2.Lerp(
							kpos[key.OldIdx],
							kpos[key.NewIdx],
							KeyEasing(LoopTime)
						) + shake, Cfg.keyRadius * SinEase01(beats / 8) * (onRight ? SinEase01(-beats / 8 + 1) * 2 + 1 : 1));
					Color clr = Color.White;
					// Visual conditions apply to the correct key (coloring & thickening)
					if (beats <= 8)
                    {
						if (onRight)
						{
							clr = Color.Lerp(Color.White, Color.Green, SinPeriod01(beats * 0.25f));
						}
                    }
					if (beats >= 24)
                    {
						GraphicsDevice.Clear(Color.Red * ((beats - 24) / 64));
                    }
					if (beats >= 40)
					{
						GraphicsDevice.Clear(Color.DarkSlateBlue * (-(beats - 40) * 0.0625f + 0.5f));
						clr = Rainbow(key.NewIdx * MathF.Tau / 8);
						if (circ.Contains(mse.Position))
						{
							if (mse.WasButtonJustDown(MouseButton.Left))
							{
								ChosenKey = i;
								HasChosen = true;
								FlashTime = 1;
								ChosenTime = 0;
							}
							else
							{
								clr = Color.Green;
							}
						}
					}
					float thickness;
					if (beats <= 8)
                    {
						thickness = onRight ? MathHelper.Lerp(30, 10, SinEase(beats / 8)) : 10;
						thickness = MathHelper.Lerp(0, thickness, SinOutEase(beats / 8));
					}
                    else
                    {
						thickness = 10;
                    }
					if (Cfg.debug)
					{
						string debugMsg = $"{key.NewIdx}\n{Colors[key.NewIdx]}";
						Vector2 debugCenter = JBMono.MeasureString(debugMsg) / 40;

						Batch.DrawString(JBMono, debugMsg, circ.Center - debugCenter, Color.White, 0, Vector2.Zero, 0.05f, SpriteEffects.None, 0);
					}
					Batch.DrawCircle(new CircleF(circ.Center, circ.Radius + thickness), 7, Color.Black, thickness * 3, 0.0f);
					Batch.DrawCircle(circ, 7, clr, thickness, 0.0f);
					i++;
				}
				if (beats >= 45)
				{
					string msg = "Choose your key.";
					Vector2 center = JBMono.MeasureString(msg) / 20 * Cfg.textScale;
					Batch.DrawString(JBMono, msg, new Vector2(800, 50) * Cfg.winScale - center, Color.White * ((beats - 45) / 5), 0, Vector2.Zero, 0.1f * Cfg.textScale, SpriteEffects.None, 0);
				}
			}
		}
		Batch.End();
		base.Draw(gameTime);
	}
}
