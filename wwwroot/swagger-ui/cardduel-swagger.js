(function () {
  const storageKey = "cardduel.swagger.profiles.v1";
  const activeKey = "cardduel.swagger.activeProfile";
  const variableKeys = ["playerId", "deckId", "matchId", "roomCode", "reconnectToken", "rulesetId", "opponentId", "cardId", "abilityId", "profileKey", "deckEntryId", "itemTypeKey", "playerCardId", "upgradeId", "requirementId", "runtimeCardId"];
  const defaultProfiles = [
    {
      name: "Player One",
      email: "playerone@flippy.com",
      password: "123456",
      playerId: "user-1",
      deckId: "deck_playerone_1",
      token: "",
      matchId: "",
      roomCode: "",
      reconnectToken: "",
      rulesetId: "",
      opponentId: "user-2",
      cardId: "card_001",
      abilityId: "poison",
      profileKey: "hand-default",
      deckEntryId: "",
      itemTypeKey: "card_dust",
      playerCardId: "",
      upgradeId: "",
      requirementId: "",
      runtimeCardId: ""
    },
    {
      name: "Player Two",
      email: "playertwo@flippy.com",
      password: "123456",
      playerId: "user-2",
      deckId: "deck_playertwo_1",
      token: "",
      matchId: "",
      roomCode: "",
      reconnectToken: "",
      rulesetId: "",
      opponentId: "user-1",
      cardId: "card_001",
      abilityId: "poison",
      profileKey: "hand-default",
      deckEntryId: "",
      itemTypeKey: "card_dust",
      playerCardId: "",
      upgradeId: "",
      requirementId: "",
      runtimeCardId: ""
    }
  ];

  function loadProfiles() {
    try {
      const parsed = JSON.parse(localStorage.getItem(storageKey) || "[]");
      return Array.isArray(parsed) && parsed.length > 0 ? parsed : defaultProfiles;
    } catch {
      return defaultProfiles;
    }
  }

  function saveProfiles(profiles) {
    localStorage.setItem(storageKey, JSON.stringify(profiles));
  }

  function activeIndex(profiles) {
    const saved = Number.parseInt(localStorage.getItem(activeKey) || "0", 10);
    return Number.isFinite(saved) && saved >= 0 && saved < profiles.length ? saved : 0;
  }

  function bareToken(value) {
    return (value || "").replace(/^Bearer\s+/i, "").trim();
  }

  function authorize(token, attempts) {
    const clean = bareToken(token);
    const retryCount = attempts || 0;
    if (!clean) {
      return;
    }

    if (!window.ui || typeof window.ui.preauthorizeApiKey !== "function") {
      if (retryCount < 20) {
        window.setTimeout(() => authorize(clean, retryCount + 1), 150);
      }

      return;
    }

    window.ui.preauthorizeApiKey("Bearer", clean);
  }

  function readProfile(form) {
    return {
      name: form.querySelector("[data-field='name']").value.trim() || "Unnamed profile",
      email: form.querySelector("[data-field='email']").value.trim(),
      password: form.querySelector("[data-field='password']").value,
      playerId: form.querySelector("[data-field='playerId']").value.trim(),
      deckId: form.querySelector("[data-field='deckId']").value.trim(),
      token: bareToken(form.querySelector("[data-field='token']").value),
      matchId: form.querySelector("[data-field='matchId']").value.trim(),
      roomCode: form.querySelector("[data-field='roomCode']").value.trim(),
      reconnectToken: form.querySelector("[data-field='reconnectToken']").value.trim(),
      rulesetId: form.querySelector("[data-field='rulesetId']").value.trim(),
      opponentId: form.querySelector("[data-field='opponentId']").value.trim(),
      cardId: form.querySelector("[data-field='cardId']").value.trim(),
      abilityId: form.querySelector("[data-field='abilityId']").value.trim(),
      profileKey: form.querySelector("[data-field='profileKey']").value.trim(),
      deckEntryId: form.querySelector("[data-field='deckEntryId']").value.trim(),
      itemTypeKey: form.querySelector("[data-field='itemTypeKey']").value.trim(),
      playerCardId: form.querySelector("[data-field='playerCardId']").value.trim(),
      upgradeId: form.querySelector("[data-field='upgradeId']").value.trim(),
      requirementId: form.querySelector("[data-field='requirementId']").value.trim(),
      runtimeCardId: form.querySelector("[data-field='runtimeCardId']").value.trim()
    };
  }

  function writeProfile(form, profile) {
    for (const [key, value] of Object.entries(profile)) {
      const field = form.querySelector(`[data-field='${key}']`);
      if (field) {
        field.value = value || "";
      }
    }
  }

  function renderProfileOptions(select, profiles, selectedIndex) {
    select.innerHTML = "";
    profiles.forEach((profile, index) => {
      const option = document.createElement("option");
      option.value = String(index);
      option.textContent = profile.name || `Profile ${index + 1}`;
      select.appendChild(option);
    });
    select.value = String(selectedIndex);
  }

  function setStatus(root, message) {
    root.querySelector("[data-role='status']").textContent = message;
  }

  async function postJson(path, payload) {
    const response = await fetch(path, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload)
    });

    const text = await response.text();
    const data = text ? JSON.parse(text) : {};
    if (!response.ok) {
      throw new Error(data.message || data.title || `HTTP ${response.status}`);
    }

    return data;
  }

  function copyVariables(profile) {
    const text = [
      ...variableKeys.map(key => `${key}=${profile[key] || ""}`),
      `Authorization=Bearer ${profile.token || ""}`
    ].join("\n");

    return navigator.clipboard.writeText(text);
  }

  async function getJson(path, authorized) {
    const headers = {};
    const token = window.cardDuelSwagger && window.cardDuelSwagger.getToken ? window.cardDuelSwagger.getToken() : "";
    if (authorized && token) {
      headers.Authorization = `Bearer ${token}`;
    }

    const response = await fetch(window.cardDuelSwagger.replaceVariables(path), { headers });
    const text = await response.text();
    const data = text ? JSON.parse(text) : {};
    if (!response.ok) {
      throw new Error(data.message || data.title || `HTTP ${response.status}`);
    }

    return data;
  }

  function fillSelect(select, items, valueSelector, labelSelector, emptyLabel) {
    if (!select) {
      return;
    }

    select.innerHTML = "";
    const empty = document.createElement("option");
    empty.value = "";
    empty.textContent = emptyLabel;
    select.appendChild(empty);

    for (const item of items) {
      const option = document.createElement("option");
      option.value = valueSelector(item);
      option.textContent = labelSelector(item);
      select.appendChild(option);
    }
  }

  async function safeFill(root, pickerName, path, valueSelector, labelSelector, emptyLabel, authorized) {
    const select = root.querySelector(`[data-picker='${pickerName}']`);
    if (!select) {
      return;
    }

    try {
      const items = await getJson(path, authorized);
      fillSelect(select, items, valueSelector, labelSelector, emptyLabel);
    } catch {
      fillSelect(select, [], () => "", () => "", `${emptyLabel} (sin datos)`);
    }
  }

  async function refreshPickers(root) {
    await safeFill(
      root,
      "cardId",
      "/api/v1/cards",
      card => card.cardId || card.CardId,
      card => `${card.cardId || card.CardId} - ${card.displayName || card.DisplayName}`,
      "Selecciona una carta desde DB",
      false);

    await safeFill(
      root,
      "abilityId",
      "/api/v1/abilities",
      ability => ability.abilityId || ability.AbilityId,
      ability => `${ability.abilityId || ability.AbilityId} - ${ability.displayName || ability.DisplayName}`,
      "Selecciona una ability desde DB",
      false);

    await safeFill(
      root,
      "profileKey",
      "/api/v1/authoring/card-visual-profile-templates",
      profile => profile.profileKey || profile.ProfileKey,
      profile => `${profile.profileKey || profile.ProfileKey} - ${profile.displayName || profile.DisplayName}`,
      "Selecciona un visual profile template",
      false);

    await safeFill(
      root,
      "itemTypeKey",
      "/api/v1/items",
      item => item.key || item.Key,
      item => `${item.key || item.Key} - ${item.displayName || item.DisplayName}`,
      "Selecciona un item type",
      false);

    const lookups = await getJson("/api/v1/authoring/lookups", false);
    fillSelect(root.querySelector("[data-picker='effectKind']"), lookups.effectKinds || lookups.EffectKinds || [], effect => effect.id ?? effect.Id, effect => `${effect.id ?? effect.Id}: ${effect.key || effect.Key} - ${effect.displayName || effect.DisplayName}`, "Referencia effect kind");
    fillSelect(root.querySelector("[data-picker='targetSelector']"), lookups.targetSelectors || lookups.TargetSelectors || [], selector => selector.id ?? selector.Id, selector => `${selector.id ?? selector.Id}: ${selector.key || selector.Key}`, "Referencia target selector");

    const active = readProfile(root);
    if (active.playerId && active.deckId && active.token) {
      const deck = await getJson(`/api/v1/decks/${encodeURIComponent(active.playerId)}/${encodeURIComponent(active.deckId)}`, true);
      fillSelect(
        root.querySelector("[data-picker='deckEntryId']"),
        deck.cards || deck.Cards || [],
        entry => entry.entryId || entry.EntryId,
        entry => `${entry.position ?? entry.Position}: ${entry.cardId || entry.CardId}`,
        "Selecciona entrada del deck");

      await safeFill(
        root,
        "playerCardId",
        `/api/v1/players/${encodeURIComponent(active.playerId)}/cards`,
        playerCard => playerCard.id || playerCard.Id,
        playerCard => `${playerCard.id || playerCard.Id} - ${playerCard.cardId || playerCard.CardId} - ${playerCard.displayName || playerCard.DisplayName}`,
        "Selecciona una carta owned",
        true);
    } else {
      fillSelect(root.querySelector("[data-picker='deckEntryId']"), [], () => "", () => "", "Selecciona entrada del deck");
      fillSelect(root.querySelector("[data-picker='playerCardId']"), [], () => "", () => "", "Selecciona una carta owned");
    }

    if (active.playerId && active.playerCardId && active.token) {
      await safeFill(
        root,
        "upgradeId",
        `/api/v1/players/${encodeURIComponent(active.playerId)}/cards/${encodeURIComponent(active.playerCardId)}/upgrades`,
        upgrade => upgrade.id || upgrade.Id,
        upgrade => `${upgrade.id || upgrade.Id} - ${upgrade.upgradeKind || upgrade.UpgradeKind}`,
        "Selecciona un upgrade",
        true);
    } else {
      fillSelect(root.querySelector("[data-picker='upgradeId']"), [], () => "", () => "", "Selecciona un upgrade");
    }

    if (active.cardId) {
      try {
        const recipe = await getJson(`/api/v1/crafting/cards/${encodeURIComponent(active.cardId)}`, false);
        fillSelect(
          root.querySelector("[data-picker='requirementId']"),
          recipe.requirements || recipe.Requirements || [],
          requirement => requirement.id || requirement.Id,
          requirement => `${requirement.id || requirement.Id} - ${requirement.itemTypeKey || requirement.ItemTypeKey} x${requirement.quantityRequired ?? requirement.QuantityRequired}`,
          "Selecciona un requirement");
      } catch {
        fillSelect(root.querySelector("[data-picker='requirementId']"), [], () => "", () => "", "Selecciona un requirement");
      }
    } else {
      fillSelect(root.querySelector("[data-picker='requirementId']"), [], () => "", () => "", "Selecciona un requirement");
    }

    if (active.matchId && active.playerId && active.token) {
      try {
        const snapshot = await getJson(`/api/v1/matches/${encodeURIComponent(active.matchId)}/snapshot/${encodeURIComponent(active.playerId)}`, true);
        const localSeatIndex = snapshot.localSeatIndex ?? snapshot.LocalSeatIndex;
        const seats = snapshot.seats || snapshot.Seats || [];
        const localSeat = seats.find(seat => (seat.seatIndex ?? seat.SeatIndex) === localSeatIndex);
        const runtimeCards = ((localSeat && (localSeat.board || localSeat.Board)) || [])
          .filter(slot => slot.occupied ?? slot.Occupied)
          .map(slot => {
            const occupant = slot.occupant || slot.Occupant || {};
            return {
              runtimeId: occupant.runtimeId || occupant.RuntimeId,
              cardId: occupant.cardId || occupant.CardId,
              displayName: occupant.displayName || occupant.DisplayName,
              slot: slot.slot || slot.Slot
            };
          });
        fillSelect(
          root.querySelector("[data-picker='runtimeCardId']"),
          runtimeCards,
          card => card.runtimeId,
          card => `${card.slot} - ${card.cardId} - ${card.displayName}`,
          "Selecciona una carta en juego");
      } catch {
        fillSelect(root.querySelector("[data-picker='runtimeCardId']"), [], () => "", () => "", "Selecciona una carta en juego");
      }
    } else {
      fillSelect(root.querySelector("[data-picker='runtimeCardId']"), [], () => "", () => "", "Selecciona una carta en juego");
    }
  }

  let profiles = loadProfiles();
  let selectedIndex = activeIndex(profiles);
  let helperRoot = null;

  function saveActiveProfile(profile) {
    profiles = loadProfiles();
    selectedIndex = activeIndex(profiles);
    profiles[selectedIndex] = { ...(profiles[selectedIndex] || {}), ...profile };
    saveProfiles(profiles);

    if (helperRoot) {
      const select = helperRoot.querySelector("[data-role='profileSelect']");
      renderProfileOptions(select, profiles, selectedIndex);
      writeProfile(helperRoot, profiles[selectedIndex]);
    }
  }

  function handleAuthData(data, source) {
    if (!data || !data.token) {
      return false;
    }

    const current = window.cardDuelSwagger.getActiveProfile();
    saveActiveProfile({
      ...current,
      token: data.token,
      playerId: data.userId || current.playerId,
      name: data.username || current.name,
      email: data.email || current.email
    });

    authorize(data.token);

    if (helperRoot) {
      setStatus(helperRoot, `${source || "Auth"} OK: token guardado y Swagger autorizado`);
    }

    return true;
  }

  function installHelper() {
    if (document.querySelector(".cardduel-helper")) {
      return;
    }

    const swaggerRoot = document.querySelector(".swagger-ui");
    if (!swaggerRoot) {
      window.setTimeout(installHelper, 250);
      return;
    }

    const information = swaggerRoot.querySelector(".information-container");
    const profile = profiles[selectedIndex];

    const root = document.createElement("section");
    root.className = "cardduel-helper";
    root.innerHTML = `
      <div class="cardduel-helper__header">
        <div>
          <h2>CardDuel Swagger Helper</h2>
          <p>Login/register autoriza Swagger automaticamente. Los perfiles y variables globales viven en localStorage y sobreviven refresh del navegador.</p>
        </div>
        <div class="cardduel-helper__status" data-role="status">Ready</div>
      </div>
      <div class="cardduel-helper__body">
        <form class="cardduel-helper__section" data-role="form">
          <h3>Perfil y auth</h3>
          <label>Perfil guardado<select data-role="profileSelect"></select></label>
          <div class="cardduel-helper__grid">
            <label>Nombre<input data-field="name" autocomplete="off"></label>
            <label>Email<input data-field="email" autocomplete="username"></label>
            <label>Password<input data-field="password" type="password" autocomplete="current-password"></label>
            <label>Player ID<input data-field="playerId" placeholder="{{playerId}}"></label>
            <label>Deck ID<input data-field="deckId" placeholder="deck_playerone_1"></label>
            <label>Opponent ID<input data-field="opponentId" placeholder="{{opponentId}}"></label>
            <label>Item Type Key<input data-field="itemTypeKey" placeholder="{{itemTypeKey}}"></label>
            <label>Player Card ID<input data-field="playerCardId" placeholder="{{playerCardId}}"></label>
          </div>
          <label>JWT token<textarea data-field="token" placeholder="Login lo llena automaticamente"></textarea></label>
          <div class="cardduel-helper__actions">
            <button type="button" data-action="login">Login + autorizar</button>
            <button type="button" data-action="register" class="secondary">Register + autorizar</button>
            <button type="button" data-action="save" class="ghost">Guardar perfil</button>
            <button type="button" data-action="new" class="ghost">Nuevo perfil</button>
            <button type="button" data-action="authorize" class="ghost">Autorizar token</button>
          </div>
        </form>
        <div class="cardduel-helper__section">
          <h3>Variables globales persistentes</h3>
          <div class="cardduel-helper__grid">
            <label>Match ID<input data-field="matchId" form="cardduel-none" placeholder="{{matchId}}"></label>
            <label>Room Code<input data-field="roomCode" form="cardduel-none" placeholder="{{roomCode}}"></label>
            <label>Reconnect Token<input data-field="reconnectToken" form="cardduel-none" placeholder="{{reconnectToken}}"></label>
            <label>Ruleset ID<input data-field="rulesetId" form="cardduel-none" placeholder="{{rulesetId}}"></label>
            <label>Card ID<input data-field="cardId" form="cardduel-none" placeholder="{{cardId}}"></label>
            <label>Ability ID<input data-field="abilityId" form="cardduel-none" placeholder="{{abilityId}}"></label>
            <label>Visual Profile Key<input data-field="profileKey" form="cardduel-none" placeholder="{{profileKey}}"></label>
            <label>Deck Entry ID<input data-field="deckEntryId" form="cardduel-none" placeholder="{{deckEntryId}}"></label>
            <label>Upgrade ID<input data-field="upgradeId" form="cardduel-none" placeholder="{{upgradeId}}"></label>
            <label>Requirement ID<input data-field="requirementId" form="cardduel-none" placeholder="{{requirementId}}"></label>
            <label>Runtime Card ID<input data-field="runtimeCardId" form="cardduel-none" placeholder="{{runtimeCardId}}"></label>
          </div>
          <div class="cardduel-helper__actions">
            <button type="button" data-action="saveVariables">Guardar variables</button>
            <button type="button" data-action="copyVariables" class="ghost">Copiar variables</button>
            <button type="button" data-action="clearToken" class="ghost">Limpiar token</button>
          </div>
          <p class="cardduel-helper__hint">Placeholders soportados y persistentes: <code>{{playerId}}</code>, <code>{{deckId}}</code>, <code>{{matchId}}</code>, <code>{{roomCode}}</code>, <code>{{reconnectToken}}</code>, <code>{{rulesetId}}</code>, <code>{{opponentId}}</code>, <code>{{cardId}}</code>, <code>{{abilityId}}</code>, <code>{{profileKey}}</code>, <code>{{deckEntryId}}</code>, <code>{{itemTypeKey}}</code>, <code>{{playerCardId}}</code>, <code>{{upgradeId}}</code>, <code>{{requirementId}}</code>, <code>{{runtimeCardId}}</code>. El interceptor los reemplaza antes de enviar cada request.</p>
        </div>
        <div class="cardduel-helper__section cardduel-helper__section--wide">
          <h3>Pickers desde base de datos</h3>
          <p class="cardduel-helper__hint">Estos selectores consultan la API y guardan variables persistentes. Es lo mas cercano a un dropdown dinamico real en Swagger sin escribir un plugin completo de Swagger UI.</p>
          <div class="cardduel-helper__grid cardduel-helper__grid--five">
            <label>Carta<select data-picker="cardId"></select></label>
            <label>Ability<select data-picker="abilityId"></select></label>
            <label>Visual Template<select data-picker="profileKey"></select></label>
            <label>Deck Entry<select data-picker="deckEntryId"></select></label>
            <label>Item Type<select data-picker="itemTypeKey"></select></label>
            <label>Owned Card<select data-picker="playerCardId"></select></label>
            <label>Upgrade<select data-picker="upgradeId"></select></label>
            <label>Craft Requirement<select data-picker="requirementId"></select></label>
            <label>Runtime Board Card<select data-picker="runtimeCardId"></select></label>
            <label>Effect kind<select data-picker="effectKind"></select></label>
            <label>Target selector<select data-picker="targetSelector"></select></label>
          </div>
          <div class="cardduel-helper__actions">
            <button type="button" data-action="refreshPickers">Actualizar pickers</button>
          </div>
        </div>
      </div>
    `;

    helperRoot = root;

    if (information && information.parentNode) {
      information.parentNode.insertBefore(root, information.nextSibling);
    } else {
      swaggerRoot.insertBefore(root, swaggerRoot.firstChild);
    }

    const form = root.querySelector("[data-role='form']");
    const select = root.querySelector("[data-role='profileSelect']");
    renderProfileOptions(select, profiles, selectedIndex);
    writeProfile(root, profile);
    authorize(profile.token);
    refreshPickers(root).catch(() => {});

    select.addEventListener("change", () => {
      selectedIndex = Number.parseInt(select.value, 10);
      localStorage.setItem(activeKey, String(selectedIndex));
      writeProfile(root, profiles[selectedIndex]);
      authorize(profiles[selectedIndex].token);
      setStatus(root, `Perfil activo: ${profiles[selectedIndex].name}`);
      refreshPickers(root).catch(() => {});
    });

    root.addEventListener("click", async (event) => {
      const button = event.target.closest("button[data-action]");
      if (!button) {
        return;
      }

      const action = button.getAttribute("data-action");
      try {
        if (action === "save" || action === "saveVariables") {
          profiles[selectedIndex] = readProfile(root);
          saveProfiles(profiles);
          renderProfileOptions(select, profiles, selectedIndex);
          setStatus(root, "Perfil guardado");
        }

        if (action === "refreshPickers") {
          await refreshPickers(root);
          setStatus(root, "Pickers actualizados desde la API");
        }

        if (action === "new") {
          profiles.push({ ...defaultProfiles[0], name: `Profile ${profiles.length + 1}`, email: "", password: "", token: "" });
          selectedIndex = profiles.length - 1;
          saveProfiles(profiles);
          localStorage.setItem(activeKey, String(selectedIndex));
          renderProfileOptions(select, profiles, selectedIndex);
          writeProfile(root, profiles[selectedIndex]);
          setStatus(root, "Nuevo perfil creado");
        }

        if (action === "login") {
          const current = readProfile(root);
          const data = await postJson("/api/v1/auth/login", { email: current.email, password: current.password });
          profiles[selectedIndex] = { ...current, token: data.token, playerId: data.userId || current.playerId, name: data.username || current.name, email: data.email || current.email };
          saveProfiles(profiles);
          writeProfile(root, profiles[selectedIndex]);
          handleAuthData(data, "Login");
          refreshPickers(root).catch(() => {});
        }

        if (action === "register") {
          const current = readProfile(root);
          const username = current.name.replace(/\s+/g, "") || current.email.split("@")[0] || "SwaggerPlayer";
          const data = await postJson("/api/v1/auth/register", { email: current.email, username, password: current.password });
          profiles[selectedIndex] = { ...current, token: data.token, playerId: data.userId || current.playerId, name: data.username || current.name };
          saveProfiles(profiles);
          renderProfileOptions(select, profiles, selectedIndex);
          writeProfile(root, profiles[selectedIndex]);
          handleAuthData(data, "Register");
          refreshPickers(root).catch(() => {});
        }

        if (action === "authorize") {
          const current = readProfile(root);
          profiles[selectedIndex] = current;
          saveProfiles(profiles);
          authorize(current.token);
          setStatus(root, "Token autorizado en Swagger");
        }

        if (action === "copyVariables") {
          const current = readProfile(root);
          profiles[selectedIndex] = current;
          saveProfiles(profiles);
          await copyVariables(current);
          setStatus(root, "Variables copiadas");
        }

        if (action === "clearToken") {
          const current = readProfile(root);
          profiles[selectedIndex] = { ...current, token: "" };
          saveProfiles(profiles);
          writeProfile(root, profiles[selectedIndex]);
          setStatus(root, "Token limpiado localmente");
        }
      } catch (error) {
        setStatus(root, `Error: ${error.message}`);
      }
    });

    root.addEventListener("change", (event) => {
      const picker = event.target.closest("select[data-picker]");
      if (!picker || !picker.value) {
        return;
      }

      const fieldName = picker.getAttribute("data-picker");
      const field = root.querySelector(`[data-field='${fieldName}']`);
      if (field) {
        field.value = picker.value;
        profiles[selectedIndex] = readProfile(root);
        saveProfiles(profiles);
        setStatus(root, `${fieldName} guardado: ${picker.value}`);
      }
    });
  }

  window.cardDuelSwagger = {
    getActiveProfile: function () {
      const profiles = loadProfiles();
      return profiles[activeIndex(profiles)] || {};
    },
    replaceVariables: function (value) {
      const profile = this.getActiveProfile();
      let result = value || "";
      for (const key of [...variableKeys, "userId"]) {
        const replacement = key === "userId" ? (profile.playerId || "") : (profile[key] || "");
        const token = `{{${key}}}`;
        const lowerToken = token.toLowerCase();
        const encodedToken = encodeURIComponent(token);
        const encodedLowerToken = encodeURIComponent(lowerToken);
        const doubleEncodedToken = encodeURIComponent(encodedToken);
        const doubleEncodedLowerToken = encodeURIComponent(encodedLowerToken);

        result = result
          .split(token).join(replacement)
          .split(lowerToken).join(replacement)
          .split(encodedToken).join(encodeURIComponent(replacement))
          .split(encodedToken.toLowerCase()).join(encodeURIComponent(replacement))
          .split(encodedLowerToken).join(encodeURIComponent(replacement))
          .split(encodedLowerToken.toLowerCase()).join(encodeURIComponent(replacement))
          .split(doubleEncodedToken).join(encodeURIComponent(encodeURIComponent(replacement)))
          .split(doubleEncodedToken.toLowerCase()).join(encodeURIComponent(encodeURIComponent(replacement)))
          .split(doubleEncodedLowerToken).join(encodeURIComponent(encodeURIComponent(replacement)))
          .split(doubleEncodedLowerToken.toLowerCase()).join(encodeURIComponent(encodeURIComponent(replacement)));
      }
      return result;
    },
    getToken: function () {
      return bareToken(this.getActiveProfile().token || "");
    },
    handleAuthResponse: function (response) {
      const url = response && response.url ? response.url : "";
      if (!url.includes("/api/v1/auth/login") && !url.includes("/api/v1/auth/register")) {
        return response;
      }

      let data = response.data;
      if (typeof data === "string") {
        try {
          data = JSON.parse(data);
        } catch {
          data = null;
        }
      }

      handleAuthData(data, url.includes("/register") ? "Register" : "Login");
      return response;
    }
  };

  installHelper();
  window.addEventListener("load", installHelper);
  new MutationObserver(installHelper).observe(document.documentElement, { childList: true, subtree: true });
})();
