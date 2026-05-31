# Instalacja panelu na VPS — krok po kroku (gotowiec)

Panel działa na osobnym VPS i łączy się z grą przez **wspólną bazę MySQL**
(tę samą, której używa serwer CS2 na DatHost). Nie otwierasz żadnych portów na
serwerze gry.

---

## Krok 0 — Co musisz mieć pod ręką

Z panelu **DatHost → Databases** skopiuj:
- host bazy (np. `xxx.dathost.net`)
- port (zwykle `3306`)
- użytkownik, hasło, nazwa bazy

Oraz dane Twojego VPS (IP + dostęp SSH).

---

## Krok 1 — Wgraj pliki panelu na VPS

Na VPS (przez SSH):

```bash
# zainstaluj git jeśli nie ma
sudo apt-get update && sudo apt-get install -y git

# pobierz repo (lub wgraj sam folder panel/ przez scp/FTP)
git clone https://github.com/zgbln99/retakes.git
cd retakes/panel
```

> Alternatywnie, jeśli nie chcesz klonować całego repo: skopiuj na VPS sam
> folder `panel/` (np. `scp -r panel user@VPS_IP:~/cwelownia-panel`).

---

## Krok 2 — Uruchom instalator (robi wszystko za Ciebie)

```bash
sudo bash install.sh
```

Instalator:
- zainstaluje Node.js (jeśli go nie ma),
- zainstaluje zależności (`npm install`),
- utworzy plik `.env` z losowym `SESSION_SECRET`,
- skonfiguruje usługę `systemd` (autostart po restarcie VPS).

---

## Krok 3 — Uzupełnij dane

```bash
nano .env
```

Wpisz dane MySQL z DatHost oraz login/hasło do panelu:

```
DB_HOST=xxx.dathost.net
DB_PORT=3306
DB_USER=uzytkownik
DB_PASSWORD=haslo
DB_NAME=nazwa_bazy
DB_PREFIX=retakes_
SERVER_ID=cwelownia1
PORT=8080
ADMIN_USER=admin
ADMIN_PASSWORD=mocne-haslo-do-panelu
```

> `SERVER_ID` musi być **taki sam** jak `RemoteControlSettings.ServerId`
> w configu pluginu na serwerze gry.

---

## Krok 4 — Włącz most w pluginie (serwer gry)

W `addons/counterstrikesharp/configs/plugins/RetakesPlugin/RetakesPlugin.json`:

```json
"RemoteControlSettings": {
  "IsEnabled": true,
  "ServerId": "cwelownia1"
}
```

Dane MySQL plugin bierze z `StatsSettings.Database` — upewnij się, że są
wpisane (te same co w panelu). Zrestartuj serwer gry.

---

## Krok 5 — Start panelu

```bash
sudo systemctl start cwelownia-panel
systemctl status cwelownia-panel      # czy działa
journalctl -u cwelownia-panel -f      # podgląd logów na żywo
```

Wejdź w przeglądarce: `http://VPS_IP:8080` → zaloguj się danymi z `.env`.

---

## Krok 6 (zalecane) — HTTPS przez nginx

```bash
sudo apt-get install -y nginx certbot python3-certbot-nginx
```

`/etc/nginx/sites-available/cwelownia`:

```nginx
server {
    server_name panel.twojadomena.pl;
    location / {
        proxy_pass http://127.0.0.1:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}
```

```bash
sudo ln -s /etc/nginx/sites-available/cwelownia /etc/nginx/sites-enabled/
sudo nginx -t && sudo systemctl reload nginx
sudo certbot --nginx -d panel.twojadomena.pl
```

Po tym otwórz porty tylko dla nginx (80/443) i ewentualnie zamknij 8080 z zewnątrz.

---

## Aktualizacja panelu

```bash
cd retakes && git pull
cd panel && npm install --omit=dev
sudo systemctl restart cwelownia-panel
```

## Najczęstsze problemy

- **Panel nie pokazuje statusu/graczy** → sprawdź czy plugin ma
  `RemoteControlSettings.IsEnabled = true`, ten sam `ServerId`, i czy w logu
  serwera jest `[Remote] ... bridge initialized`.
- **Błąd połączenia z bazą** → złe dane w `.env` lub baza nie przyjmuje
  połączeń z IP Twojego VPS (whitelist w DatHost).
- **Komendy nic nie robią** → spójrz na zakładkę „Historia komend": status
  `done` = wykonane, `rejected` = poza allowlistą, `running/pending` = plugin
  jeszcze ich nie odczytał (czekaj kilka sekund / sprawdź czy serwer żyje).
