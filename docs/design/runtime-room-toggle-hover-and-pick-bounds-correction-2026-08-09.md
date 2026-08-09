# Runtime room toggle, hover and precise pick bounds correction

Статус: `APPROVED`.

Последнее подтверждённое решение пользователя: 2026-08-09.

Tracking issue: [#679](https://github.com/bageus/Dig/issues/679).

Связанные системы:

- [`contextual-input-cursors-and-selection.md`](contextual-input-cursors-and-selection.md);
- [`world-item-gravity-selection-and-pickup.md`](world-item-gravity-selection-and-pickup.md);
- [`room-purposes-upgrades-and-tunnel-reinforcement.md`](room-purposes-upgrades-and-tunnel-reinforcement.md).

## Подтверждённое поведение

1. В excavation palette существует один переключатель `Room Types`. До завершения
   первой комнаты он отсутствует. Выключенное состояние означает обычный режим копки,
   включённое показывает типы и overlay комнат.
2. Любой доступный мировой предмет, материал, гриб, бочка, существо или другой
   интерактивный объект получает hover по фактической геометрии независимо от выбора
   гнома.
3. При выбранном живом гноме доступное действие дополнительно показывает
   соответствующий анимированный cursor. Cursor является параллельной Presentation-
   анимацией: LMB немедленно отправляет command и не ожидает завершения кадра или цикла.
4. Без выбранного гнома объект сохраняет hover, но command cursor не показывается.
5. Collider поднимаемого world item повторяет bounds активной визуальной геометрии с
   техническим допуском `0.02` клетки на ось. Крупная минимальная зона взаимодействия
   не создаётся.

## Acceptance

- до первой комнаты toggle отсутствует; после неё появляется ровно одна кнопка;
- off скрывает room overlay и оставляет dig mode, on показывает room types;
- item/mushroom/barrel/hostile hover работает без выбранного гнома;
- после выбора гнома тот же exact target получает action cursor;
- click начинает command в своём input frame независимо от cursor animation;
- collider world item отличается от renderer bounds не более чем на утверждённый
  технический допуск.
