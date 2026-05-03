# Virtual Roleplay — DarkRP S&box

> Serveur DarkRP français sur S&box. Jobs, économie et roleplay dans une ville vivante.

![S&box](https://img.shields.io/badge/S%26box-Compatible-blue)
![License](https://img.shields.io/badge/License-MIT-green)
![Language](https://img.shields.io/badge/Langage-C%23-purple)

---

## À propos

**Virtual Roleplay** est un serveur DarkRP francophone basé sur S&box. Forké depuis [sousou63/DarkRP](https://github.com/sousou63/DarkRP), ce projet apporte une expérience roleplay complète avec des modifications personnalisées adaptées à notre communauté.

- Serveur : hébergé sur [low.ms](https://low.ms)
- Gamemode : `virtualroleplay/darkrp` sur sbox.game

---

## Fonctionnalités

- Jobs et métiers
- Système économique
- Système d'administration
- Interface en français
- Modifications gameplay exclusives

---

## Installation (développement local)

### Prérequis
- [S&box](https://sbox.facepunch.com/) installé
- Git

### Cloner le repo

```bash
git clone https://github.com/Lu6asM/DarkRP
```

Placer le dossier dans :
```
C:\Program Files (x86)\Steam\steamapps\common\sbox\addons\
```

Ouvrir le projet depuis l'éditeur S&box.

---

## Configuration admin

Créer le fichier `server/admins.json` avec vos Steam ID 64 :

```json
{
  "VOTRE_STEAMID64": "superadmin"
}
```

---

## Récupérer les mises à jour du repo original

```bash
git remote add upstream https://github.com/sousou63/DarkRP
git fetch upstream
git merge upstream/main
```

---

## Licence

MIT — basé sur le travail de [sousou63](https://github.com/sousou63) et [Facepunch](https://github.com/Facepunch).
