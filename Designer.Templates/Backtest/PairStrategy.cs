﻿namespace StockSharp.Designer
{
	using System;
	using System.Linq;
	using System.Collections.Generic;

	using Ecng.Common;
	using Ecng.Logging;
	using Ecng.Drawing;

	using StockSharp.Messages;
	using StockSharp.Algo;
	using StockSharp.Algo.Strategies;
	using StockSharp.Algo.Indicators;
	using StockSharp.BusinessEntities;
	using StockSharp.Localization;
	using StockSharp.Charting;

	/// <summary>
	/// Sample strategy demonstrating the work with two instruments.
	/// 
	/// See more examples https://github.com/StockSharp/AlgoTrading
	/// </summary>
	public class PairStrategy : Strategy
	{
		private bool? _isShortLessThenLong;

		public PairStrategy()
		{
			_timeFrame = Param(nameof(TimeFrame), TimeSpan.FromMinutes(5));

			_security1 = Param<Security>(nameof(Security1));
			_security2 = Param<Security>(nameof(Security2));

			_takeValue = Param(nameof(TakeValue), new Unit(0, UnitTypes.Absolute));
			_stopValue = Param(nameof(StopValue), new Unit(2, UnitTypes.Percent));
		}

		private readonly StrategyParam<TimeSpan> _timeFrame;

		public TimeSpan TimeFrame
		{
			get => _timeFrame.Value;
			set => _timeFrame.Value = value;
		}

		private readonly StrategyParam<Security> _security1;

		public Security Security1
		{
			get => _security1.Value;
			set => _security1.Value = value;
		}

		private readonly StrategyParam<Security> _security2;

		public Security Security2
		{
			get => _security2.Value;
			set => _security2.Value = value;
		}

		private readonly StrategyParam<int> _long;

		public int Long
		{
			get => _long.Value;
			set => _long.Value = value;
		}

		private readonly StrategyParam<int> _short;

		public int Short
		{
			get => _short.Value;
			set => _short.Value = value;
		}

		private readonly StrategyParam<Unit> _takeValue;

		public Unit TakeValue
		{
			get => _takeValue.Value;
			set => _takeValue.Value = value;
		}

		private readonly StrategyParam<Unit> _stopValue;

		public Unit StopValue
		{
			get => _stopValue.Value;
			set => _stopValue.Value = value;
		}

		protected override void OnReseted()
		{
			base.OnReseted();

			_isShortLessThenLong = null;
		}

		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			// ---------- create indicators -----------

			var longSma = new SMA { Length = Long };
			var shortSma = new SMA { Length = Short };

			// ----------------------------------------

			// --- bind candles set and indicators ----

			var subscription = SubscribeCandles(TimeFrame, security: Security1)
				// bind indicators to the candles
				.Bind(longSma, shortSma, OnProcess)
				// start processing
				.Start();

			// ----------------------------------------

			// ----------- configure chart ------------

			var area = CreateChartArea();

			// area can be null in case of no GUI (strategy hosted in Runner or in own console app)
			if (area != null)
			{
				DrawCandles(area, subscription);

				DrawIndicator(area, shortSma, System.Drawing.Color.Coral);
				DrawIndicator(area, longSma);

				DrawOwnTrades(area);
			}

			// ----------------------------------------

			// ---- configure position protection -----

			// start protection by take profit and-or stop loss
			StartProtection(TakeValue, StopValue);

			// ----------------------------------------
		}

		private void OnProcess(ICandleMessage candle, decimal longValue, decimal shortValue)
		{
			LogInfo(LocalizedStrings.SmaNewCandleLog, candle.OpenTime, candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice, candle.TotalVolume, candle.SecurityId);

			// in case we subscribed on non finished only candles
			if (candle.State != CandleStates.Finished)
				return;

			// calc new values for short and long
			var isShortLessThenLong = shortValue < longValue;

			if (_isShortLessThenLong == null)
			{
				_isShortLessThenLong = isShortLessThenLong;
			}
			else if (_isShortLessThenLong != isShortLessThenLong)
			{
				// crossing happened

				// if short less than long, the sale, otherwise buy
				var direction = isShortLessThenLong ? Sides.Sell : Sides.Buy;

				// calc size for open position or revert
				var volume = Position == 0 ? Volume : Position.Abs().Min(Volume) * 2;

				var priceStep = GetSecurity().PriceStep ?? 1;

				// calc order price as a close price + offset
				var price = candle.ClosePrice + (direction == Sides.Buy ? priceStep : -priceStep);

				if (direction == Sides.Buy)
					BuyLimit(price, volume);
				else
					SellLimit(price, volume);

				// store current values for short and long
				_isShortLessThenLong = isShortLessThenLong;
			}
		}
	}
}