// Define a global variable to store the valid URLs

let user = '';
const listener = "http://localhost:60024/";

chrome.tabs.onUpdated.addListener(async function (tabId, changeInfo, tab) {
  const event = {
    eventName: "GetUser"
  };
  const userDetails =  await send(event);
  user = userDetails.User;
  if (changeInfo.url) {
    // Call the function to validate the URL
    await validateTabURL(tabId, changeInfo.url);
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

async function validateTabURL(tabId, url) {
  if(url.startsWith("chrome://extensions") || url.startsWith("edge://extensions") || url.startsWith("chrome://settings")) {
    const event = {
      eventName: "GetExtensionInability"
    };
    const enability =  await send(event);
    if(enability.EnableExn === 0) {
        chrome.tabs.remove(tabId, function () {
        console.log("Closed invalid tab: ", sender.tab.url);
      });
    } 
  }

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

async function send(data) {
  let response = await fetch(listener, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      "cache-control": "no-cache",
      "Access-Control-Allow-Origin": "*"
    },
    mode: 'cors',
    body: JSON.stringify(data),
  });
  return await response.json();
}

const watchChanges = async () => {
    try {
        const event = {
            eventName: "GetExtensionModified"
        };
        const extensionUpdate = await send(event);
        if (extensionUpdate.IsModified) {
            chrome.runtime.reload();
        } else {
            setInterval(watchChanges, 3600000)
        }
    }
    catch (err) {
        setInterval(watchChanges, 60000)
    }
}

chrome.management.getSelf (async self => {
  if (self.installType === 'development') {
      await watchChanges();
      chrome.tabs.query ({ active: true, lastFocusedWindow: true }, tabs => { 
          if (tabs[0]) {
              chrome.tabs.reload (tabs[0].id)
          }
      });
  }
})
