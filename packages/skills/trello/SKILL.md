# Trello

Full Trello board management: boards, lists, cards, comments, members, labels, and checklists via the Trello REST API.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## Boards

### list_boards

List all boards for the authenticated user.

```
trello list_boards --filter open
```

| Argument | Type   | Required | Default | Description                              |
| -------- | ------ | -------- | ------- | ---------------------------------------- |
| `filter` | string | no       | open    | Board filter: `all`, `open`, `closed`, `starred` |

Returns: array of `{ id, name, desc, url, closed, starred, id_organization }`.

### get_board

Get details of a board.

```
trello get_board --board_id "abc123"
```

| Argument   | Type   | Required | Description |
| ---------- | ------ | -------- | ----------- |
| `board_id` | string | yes      | Board ID    |

Returns: `id`, `name`, `desc`, `url`, `closed`, `prefs`.

### create_board

Create a new board.

```
trello create_board --name "Product Roadmap" --desc "Q3-Q4 planning" --default_lists false
```

| Argument        | Type    | Required | Default | Description                         |
| --------------- | ------- | -------- | ------- | ----------------------------------- |
| `name`          | string  | yes      |         | Board name                          |
| `desc`          | string  | no       |         | Board description                   |
| `default_lists` | boolean | no       | true    | Create default To Do/Doing/Done lists |
| `id_organization` | string | no      |         | Organization ID to add board to     |

Returns: `id`, `name`, `url`.

### update_board

Update a board.

```
trello update_board --board_id "abc123" --name "New Name" --closed true
```

| Argument   | Type    | Required | Description                |
| ---------- | ------- | -------- | -------------------------- |
| `board_id` | string  | yes      | Board ID                   |
| `name`     | string  | no       | New name                   |
| `desc`     | string  | no       | New description            |
| `closed`   | boolean | no       | Archive/unarchive the board |

Returns: `id`, `name`.

## Lists

### list_lists

Get all lists on a board.

```
trello list_lists --board_id "abc123" --filter open
```

| Argument   | Type   | Required | Default | Description                      |
| ---------- | ------ | -------- | ------- | -------------------------------- |
| `board_id` | string | yes      |         | Board ID                         |
| `filter`   | string | no       | open    | Filter: `all`, `open`, `closed`  |

Returns: array of `{ id, name, closed, pos, id_board }`.

### create_list

Create a new list on a board.

```
trello create_list --board_id "abc123" --name "In Review"
```

| Argument   | Type   | Required | Description  |
| ---------- | ------ | -------- | ------------ |
| `board_id` | string | yes      | Board ID     |
| `name`     | string | yes      | List name    |

Returns: `id`, `name`, `pos`.

### update_list

Update a list.

```
trello update_list --list_id "abc123" --name "Done" --closed true
```

| Argument   | Type    | Required | Description        |
| ---------- | ------- | -------- | ------------------ |
| `list_id`  | string  | yes      | List ID            |
| `name`     | string  | no       | New name           |
| `closed`   | boolean | no       | Archive/unarchive  |

Returns: `id`, `name`.

## Cards

### list_cards

Get all cards on a list or board.

```
trello list_cards --list_id "abc123"
```

| Argument   | Type   | Required | Description              |
| ---------- | ------ | -------- | ------------------------ |
| `list_id`  | string | no       | List ID                  |
| `board_id` | string | no       | Board ID (alternative)   |
| `filter`   | string | no       | Filter: `all`, `open`, `closed` |

Returns: array of `{ id, name, desc, url, due, closed, id_list, id_members, id_labels, pos }`.

### get_card

Get details of a card.

```
trello get_card --card_id "abc123"
```

| Argument  | Type   | Required | Description |
| --------- | ------ | -------- | ----------- |
| `card_id` | string | yes      | Card ID     |

Returns: `id`, `name`, `desc`, `url`, `due`, `due_complete`, `closed`, `id_list`, `id_board`, `id_members`, `id_labels`, `pos`.

### create_card

Create a new card.

```
trello create_card --list_id "abc123" --name "Fix login bug" --desc "Fails on Safari" --due "2025-07-01T00:00:00.000Z"
```

| Argument     | Type         | Required | Description                  |
| ------------ | ------------ | -------- | ---------------------------- |
| `list_id`    | string       | yes      | List ID to add card to       |
| `name`       | string       | yes      | Card name                    |
| `desc`       | string       | no       | Card description             |
| `due`        | string       | no       | Due date (ISO 8601)          |
| `id_members` | string array | no       | Member IDs to assign         |
| `id_labels`  | string array | no       | Label IDs to attach          |
| `pos`        | string       | no       | Position: `top`, `bottom`    |

Returns: `id`, `name`, `url`, `id_list`.

### update_card

Update a card.

```
trello update_card --card_id "abc123" --name "Updated title" --due_complete true
```

| Argument       | Type    | Required | Description                   |
| -------------- | ------- | -------- | ----------------------------- |
| `card_id`      | string  | yes      | Card ID                       |
| `name`         | string  | no       | New name                      |
| `desc`         | string  | no       | New description               |
| `closed`       | boolean | no       | Archive/unarchive             |
| `id_list`      | string  | no       | Move to list (list ID)        |
| `due`          | string  | no       | New due date (ISO 8601)       |
| `due_complete` | boolean | no       | Mark due date complete        |

Returns: `id`, `name`, `id_list`.

### delete_card

Delete a card.

```
trello delete_card --card_id "abc123"
```

| Argument  | Type   | Required | Description |
| --------- | ------ | -------- | ----------- |
| `card_id` | string | yes      | Card ID     |

Returns: `success: true`.

### move_card

Move a card to a different list.

```
trello move_card --card_id "abc123" --list_id "xyz789" --pos "top"
```

| Argument  | Type   | Required | Default | Description              |
| --------- | ------ | -------- | ------- | ------------------------ |
| `card_id` | string | yes      |         | Card ID                  |
| `list_id` | string | yes      |         | Destination list ID      |
| `pos`     | string | no       | bottom  | Position: top or bottom  |

Returns: `id`, `name`, `id_list`.

## Comments

### list_card_comments

List comments on a card.

```
trello list_card_comments --card_id "abc123"
```

| Argument  | Type   | Required | Description |
| --------- | ------ | -------- | ----------- |
| `card_id` | string | yes      | Card ID     |

Returns: array of `{ id, text, date, member_creator }`.

### add_card_comment

Add a comment to a card.

```
trello add_card_comment --card_id "abc123" --text "LGTM!"
```

| Argument  | Type   | Required | Description  |
| --------- | ------ | -------- | ------------ |
| `card_id` | string | yes      | Card ID      |
| `text`    | string | yes      | Comment text |

Returns: `id`, `text`, `date`.

### delete_card_comment

Delete a comment on a card.

```
trello delete_card_comment --card_id "abc123" --comment_id "def456"
```

| Argument     | Type   | Required | Description |
| ------------ | ------ | -------- | ----------- |
| `card_id`    | string | yes      | Card ID     |
| `comment_id` | string | yes      | Comment ID  |

Returns: `success: true`.

## Labels

### list_board_labels

List all labels on a board.

```
trello list_board_labels --board_id "abc123"
```

| Argument   | Type   | Required | Description |
| ---------- | ------ | -------- | ----------- |
| `board_id` | string | yes      | Board ID    |

Returns: array of `{ id, name, color, id_board }`.

### create_label

Create a label on a board.

```
trello create_label --board_id "abc123" --name "Urgent" --color "red"
```

| Argument   | Type   | Required | Description                                      |
| ---------- | ------ | -------- | ------------------------------------------------ |
| `board_id` | string | yes      | Board ID                                         |
| `name`     | string | yes      | Label name                                       |
| `color`    | string | yes      | Color: red, orange, yellow, green, blue, purple  |

Returns: `id`, `name`, `color`.

## Members

### list_board_members

List members of a board.

```
trello list_board_members --board_id "abc123"
```

| Argument   | Type   | Required | Description |
| ---------- | ------ | -------- | ----------- |
| `board_id` | string | yes      | Board ID    |

Returns: array of `{ id, username, full_name, avatar_url }`.

## Checklists

### list_card_checklists

List checklists on a card.

```
trello list_card_checklists --card_id "abc123"
```

| Argument  | Type   | Required | Description |
| --------- | ------ | -------- | ----------- |
| `card_id` | string | yes      | Card ID     |

Returns: array of `{ id, name, check_items }` where `check_items` is `{ id, name, state }[]`.

### create_checklist

Create a checklist on a card.

```
trello create_checklist --card_id "abc123" --name "Definition of Done"
```

| Argument  | Type   | Required | Description    |
| --------- | ------ | -------- | -------------- |
| `card_id` | string | yes      | Card ID        |
| `name`    | string | yes      | Checklist name |

Returns: `id`, `name`.

### add_checklist_item

Add an item to a checklist.

```
trello add_checklist_item --checklist_id "abc123" --name "Write tests" --checked false
```

| Argument        | Type    | Required | Description           |
| --------------- | ------- | -------- | --------------------- |
| `checklist_id`  | string  | yes      | Checklist ID          |
| `name`          | string  | yes      | Item name             |
| `checked`       | boolean | no       | Initial checked state |

Returns: `id`, `name`, `state`.
