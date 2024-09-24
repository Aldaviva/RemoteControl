console.log("Starting background.js");

let webSocket;
connect();

function connect(){
	webSocket = new WebSocket("ws://localhost:4772/ws");

	webSocket.onopen = event => {
		console.info("WebSocket opened");
	};

	webSocket.onmessage = async event => {
		console.debug("WebSocket received message", event);

		let request;
		try {
			request = JSON.parse(event.data);
		} catch(e){
			console.error("Error processing server request", e);
		}

		console.debug(`Handling ${request.name} command from server`);

		const response = await sendMessageToActivePage(request);

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
	return await chrome.tabs.sendMessage(activeTab.id, command);
}