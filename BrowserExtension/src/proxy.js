console.log("Starting proxy.js");

let sendResponseToServiceWorker = null;
let expectedRequestId = null;

chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {
	sendResponseToServiceWorker = sendResponse;
	expectedRequestId = request.requestId;

	console.debug("Received request from service worker, sending to page and waiting for response", request);
	window.postMessage(request);

	return true; //allow sendResponseToServiceWorker to be called asynchronously instead of synchronously closing the pipe
});

window.addEventListener("message", event => {
	// console.trace("Received event", event);
	if(sendResponseToServiceWorker && event.data.requestId === expectedRequestId && "exception" in event.data){
		const response = event.data;
		console.debug("Received response from page, sending back to service worker", response);
		sendResponseToServiceWorker(response);
		sendResponseToServiceWorker = null;
		expectedRequestId = null;
	}
}, false);