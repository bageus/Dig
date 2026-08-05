# Hi3D Dwarf — Unity Generic Rig + Animation Pack

## Содержимое

- `Models/Hi3D_Dwarf_Rigged_Animated.glb`
- Generic-скелет: 36 костей
- skin weights: до 4 влияний на вершину
- root motion отсутствует; все locomotion-клипы in-place
- именованные клипы UnityGLTF: Idle, Walk, Run, Climb, Carry, Mine, Chop, Build, Eat, Study, Rest, Attack, Hit, Death

## Циклические клипы

- `Idle`
- `Walk`
- `Run`
- `Climb`
- `Carry`
- `Build`
- `Study`
- `Rest`

## Одноразовые клипы

- `Mine`
- `Chop`
- `Eat`
- `Attack`
- `Hit`
- `Death`

## Импорт в Unity

1. Установить и включить `UnityGLTF` как импортёр `.glb`.
2. Скопировать `Assets/Hi3D_Dwarf_Rigged_Animated` в проект.
3. Выделить `.glb` и выполнить `Reimport`.
4. Использовать `Mecanim` / `Generic`.
5. На `Animator` отключить `Apply Root Motion`.
6. Раскрыть модель в Project и проверить наличие 14 отдельных `AnimationClip`.

## Рекомендованный Loop Time

Включить для: Idle, Walk, Run, Climb, Carry, Build, Study, Rest.

Отключить для: Mine, Chop, Eat, Attack, Hit, Death.

## Проверка

GLB повторно прочитан после записи. Проверены имена клипов, количество ключей, нормализация quaternion и замыкание циклических клипов. Фактическое проигрывание и качество деформации в Unity Editor необходимо проверить вручную, особенно плечи, локти, таз и бороду.
