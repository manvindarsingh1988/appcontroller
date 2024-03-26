// Define a global variable to store the valid URLs
let validUrls = [];
let user = "$user$";

chrome.tabs.onUpdated.addListener(async function (tabId, changeInfo, tab) {
  if (changeInfo.url) {
    // Call the function to validate the URL
    validateTabURL(tabId, changeInfo.url);
  }

  if (changeInfo.status === 'complete' && tab.url.includes('connect.csc.gov.in')) {
    const data = {
      User: user
    };
    chrome.tabs.sendMessage(tabId, {
      action: "validateId",
      data: data
    });
  }
});

function validateTabURL(tabId, url) {
  if (!url || url.startsWith("chrome://") || url.startsWith("edge://")) {
    return;
  }

  const userDetails = {
    Id: '',
    Date: '',
    AppName: url,
    Summary: "",
    User: user,
  };

  // Close the tab if it's not valid
  sendMessageToContentScript(tabId, userDetails, url);
}

// Function to send a message to the content script
function sendMessageToContentScript(tabId, message, urlToCheck) {
  chrome.tabs.sendMessage(tabId, {
    action: "updateData",
    data: message,
    urlToCheck: urlToCheck,
  });
}

chrome.runtime.onMessage.addListener(async (message, sender) => {
  if (message.action === "isValidUrl") {
    console.log(message, sender);
    const isValid = message.data;
    
    if (!isValid) {
      console.log("in valid URL");

      chrome.tabs.remove(sender.tab.id, function () {
        console.log("Closed invalid tab: ", sender.tab.url);
      });
    }
  }
});
