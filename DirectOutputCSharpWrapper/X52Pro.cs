using System;

namespace DirectOutputCSharpWrapper {

	public enum Leds : int {
		FireIllumination = 0,
		FireARed,
		FireAGreen,
		FireBRed,
		FireBGreen,
		FireDRed,
		FireDGreen,
		FireERed,
		FireEGreen,
		Toggle12Red,
		Toggle12Green,
		Toggle34Red,
		Toggle34Green,
		Toggle56Red,
		Toggle56Green,
		POV2Red,
 		POV2Green,
		ClutchIRed,
		ClutchIGreen,
		ThrottleIllumination
	};

	public enum LedState : int {
		On = 1,
		Off = 0
	};

	public enum Strings : int {
		FirstLine = 0,
		SecondLine,
		ThirdLine
	}

}
