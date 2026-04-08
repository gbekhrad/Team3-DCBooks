function getUserIdFromToken() {
    const token = localStorage.getItem("accessToken");
    if (!token) return null;

    const payload = JSON.parse(atob(token.split('.')[1]));
    console.log(payload);
    return payload.user_id;
}

document.addEventListener("DOMContentLoaded", async () => {
    const notLoggedInDiv = document.getElementById('not-logged-in');
    const comicsListDiv = document.getElementById('comics-list');

    const token = localStorage.getItem("accessToken");
    const userId = getUserIdFromToken();
    

    // fetch user data from your backend
    const response = await fetch(`/api/users/${userId}`);
    const user = await response.json();

    if (!token || !userId) {
        notLoggedInDiv.classList.remove('d-none');
        return;
    }

    try {
        const USE_MOCK = false;

        let checkouts;

        if (USE_MOCK) {
            checkouts = [
                {
                    checkoutId: 1,
                    comicId: 101,
                    checkoutDate: "2026-04-01T00:00:00Z",
                    dueDate: "2026-04-15T00:00:00Z",
                    ComicTitle: "Batman and Robin"
                },
                {
                    checkoutId: 2,
                    comicId: 102,
                    checkoutDate: "2026-04-01T00:00:00Z",
                    dueDate: "2026-04-15T00:00:00Z",
                    ComicTitle: "Wonder Woman"
                }
            ];
        } else {
            const response = await fetch(`/api/checkout/user/${userId}`, {
                headers: {
                    Authorization: `Bearer ${token}`
                }
            });

            checkouts = await response.json();
        }

        // Token invalid / expired
        if (response.status === 401) {
            notLoggedInDiv.classList.remove('d-none');
            return;
        }

        // No comics
        if (!checkouts || checkouts.length === 0) {
            emptyMessage.classList.remove('d-none');
            return;
        }

        // Render comics
        checkouts.forEach(c => {
            const item = document.createElement('div');
            item.className =
                'list-group-item d-flex justify-content-between align-items-center';

            item.innerHTML = `
        <div>
          <strong>Title:</strong> ${c.ComicTitle}<br>
          <small>Due: ${new Date(c.dueDate).toLocaleDateString()}</small>
        </div>
        <button id="returnButton" class="btn btn-sm btn-danger">Return</button>
      `;

            item.querySelector('button').addEventListener('click', async () => {
                await returnComic(c.checkoutId, item, token);
            });

            comicsListDiv.appendChild(item);
        });

    } catch (err) {
        console.error("Error loading checkouts:", err);
    }
});

async function returnComic(checkoutId, element, token) {
    try {
        returnButton.disabled = true;
        returnButton.textContent = "Returning...";
        const response = await fetch(`/api/checkout/${checkoutId}`, {
            method: 'PUT', // 🔥 THIS is the important fix
            headers: {
                "Authorization": `Bearer ${token}`
            }
        });

        if (response.ok) {
            element.remove();
        } else if (response.status === 409) {
            alert("This comic was already returned.");
        } else {
            alert("Failed to return comic.");
        }
    } catch (err) {
        console.error(err);
    }
}