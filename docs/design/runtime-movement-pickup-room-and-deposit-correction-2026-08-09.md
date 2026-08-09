# Runtime movement, pickup, room-mode and deposit correction

Статус: `APPROVED`.

Tracking issue: [#677](https://github.com/bageus/Dig/issues/677).

Связанные authoritative systems:

- [`resident-movement-occupancy-and-vertical-traversal.md`](resident-movement-occupancy-and-vertical-traversal.md);
- [`world-item-gravity-selection-and-pickup.md`](world-item-gravity-selection-and-pickup.md);
- [`hamsters-and-grubs-ecology.md`](hamsters-and-grubs-ecology.md);
- [`room-purposes-upgrades-and-tunnel-reinforcement.md`](room-purposes-upgrades-and-tunnel-reinforcement.md);
- [`excavation-room-templates-and-deposits.md`](excavation-room-templates-and-deposits.md).

При расхождении по correction-пунктам ниже этот документ имеет приоритет как
последнее подтверждённое решение пользователя от 2026-08-09.

## Подтверждённое поведение

1. Во время вертикального traversal визуальный корпус гнома остаётся преимущественно
   у центра тоннеля. Небольшой deterministic bias к используемой грани допустим для
   читаемости climbing pose; authoritative surface и navigation не меняются.
2. Прямой pickup и automatic hauling завершают authoritative перенос предмета,
   когда resident достиг исходной клетки и находится на полностью поддерживаемой
   горизонтальной поверхности. Local surface offset внутри этой клетки не блокирует
   commit и не оставляет бесконечную carrying-анимацию.
3. Hamster и grub/червь двигаются на 15% быстрее предыдущего утверждённого cadence.
   Fixed-point credit равен соответственно `460` и `374` за ecology step.
4. Переключатель `Dig / Room Types` всегда виден в excavation palette. `Room Types`
   недоступен до появления completed template room. Room-purpose marker и overlay
   отображаются только при активном `Room Types`; вне режима они скрыты, физическая
   отделка комнаты остаётся видимой.
5. Уже раскрытая жила остаётся частично видимой на поверхности породы после назначения
   этой клетки на копку и во время quarter excavation. Designation overlay не скрывает
   deposit decoration. Скрытая, никогда не раскрытая жила не выдаёт своё наличие.
6. После завершения последней excavation-задачи resident на вертикальной поверхности
   автоматически достигает ближайшей полностью поддерживаемой горизонтальной
   поверхности комнаты. Новый доступный job имеет приоритет над recovery.

## Acceptance

- Play Mode: vertical climb сохраняет центральный visual bias;
- Play Mode: direct pickup после подхода переносит exact stack в inventory;
- Play Mode/integration: automatic hauling после подхода выполняет acquire и delivery;
- fixed-point cadence tests подтверждают ускорение hamster/grub на 15%;
- excavation HUD всегда показывает `Dig / Room Types`, а marker/overlay скрыты вне
  room-types mode;
- revealed deposit decoration видима при designation и partial excavation;
- idle vertical resident после terminal room excavation возвращается на floor, но
  не перехватывает новый job.
