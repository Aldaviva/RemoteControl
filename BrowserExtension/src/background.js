console.log("Starting background.js");

let webSocket;
connect();

function connect() {
	webSocket = new WebSocket("ws://localhost:4772/ws");

	webSocket.onopen = event => {
		console.info("WebSocket opened");
	};

	webSocket.onmessage = async event => {
		console.debug("WebSocket received message", event);

		let request;
		try {
			request = JSON.parse(event.data);
		} catch(e) {
			console.error("Error processing server request", e);
			return;
		}

		console.debug(`Handling ${request.name} command from server`);

		const response = await sendMessageToActivePage(request);

		response.requestId = request.requestId;
		webSocket.send(JSON.stringify(response));
		console.debug("Sent response to server", response);
	};

	webSocket.onclose = event => {
		console.info("WebSocket closed, reconnecting...");
		setTimeout(connect, 1000);
	};
}

async function sendMessageToActivePage(command){
	const [activeTab] = await chrome.tabs.query({ active: true, lastFocusedWindow: true });
	try {
		const response = await chrome.tabs.sendMessage(activeTab.id, command);
		// TODO not sure what tabs.sendMessage returns if there was no listener in the content script, the documentation is too vague. it says the callback is called with no arguments, but I'm using the promise, not the callback. does it just return null or undefined? or does it reject the promise, requiring a try/catch block around the caller?
		if (response) {
			return response;
		}
	} catch(e) {}

	return {
		exception: "UnsupportedWebsite",
		url: activeTab.url
	};
}