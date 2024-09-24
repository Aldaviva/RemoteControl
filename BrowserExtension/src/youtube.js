console.log("Starting youtube.js");

class YouTubeHandler extends AbstractSiteHandler {

	get #findPlayer() {
		return document.querySelector("ytd-player.ytd-watch-flexy")?.player_;
	}

	fetchPlaybackState() {
		const playerStateObject = this.#findPlayer?.getPlayerStateObject();
		return {
			isPlaying: playerStateObject?.isOrWillBePlaying ?? false,
			canPlay: (playerStateObject?.isOrWillBePlaying || playerStateObject?.isPaused) ?? false
		};
	}

	pressButton(button) {
		const player = this.#findPlayer;
		if (player) {
			switch (button) {
				case "PLAY_PAUSE":
					if (player.getPlayerStateObject().isOrWillBePlaying) {
						player.pauseVideo();
					} else {
						player.playVideo();
					}
					break;
				case "PREVIOUS_TRACK":
					player.seekBy(this.jumpDurationMs/-1000);
					break;
				case "NEXT_TRACK":
					player.seekBy(this.jumpDurationMs/1000);
					break;
				case "STOP":
					player.pauseVideo();
					break;
				case "BAND":
					// player.toggleFullscreen();
					document.activeElement?.blur();
					// server will then send an "F" keystroke to Vivaldi, putting the video in fullscreen, since it requires an authentic mouse or keyboard input, and not a synthetic click like our extension can create
					break;
				case "MEMORY":
					// unbound
					break;
				default:
					break;
			}
		}

		return "YOUTUBE";
	}

}

new YouTubeHandler().start();