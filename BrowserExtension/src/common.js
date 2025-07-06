console.log("Starting common.js");

class AbstractSiteHandler {

	start() {
		window.addEventListener("message", event => {
			// console.trace("Received event", event);
			if (event.data.requestId && !("exception" in event.data)) {
				const request = event.data;
				console.debug("Received request from service worker via proxy", request);
				const response = this.#handleCommand(request);
				console.debug("Sending response back to service worker via proxy", response);
				window.postMessage(response);
			}
		}, false);
	}

	#handleCommand(request) {
		let response = {
			exception: null,
			requestId: request.requestId
		};

		switch (request.name) {
			case "FetchPlaybackState":
				console.debug("Fetching playback state");
				response.playbackState = this.fetchPlaybackState();
				break;
			case "PressButton":
				console.info(`Pressing ${request.button} button`);
				this.pressButton(request.button);
				response.website = this.websiteName;
				break;
			default:
				response.exception = "UnsupportedCommand";
				response.name = request.name;
				break;
		}

		return response;
	}

	fetchPlaybackState() {
		console.error("fetchPlaybackState unimplemented");
	}

	pressButton(button) {
		console.error("pressButton unimplemented");
	}

	get websiteName() {
		return null;
	}

	get jumpDurationMs() {
		return 8000;
	}

	blurPage() {
		document.activeElement?.blur();
	}

}