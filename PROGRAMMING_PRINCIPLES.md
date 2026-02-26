##Main programing principles used
1. [DRY](./AquaCare/Repositories) or Don`t Repeat Yourself: 
Every interaction with Database has its own method, methods that return class instances use Map method for necessary class instance
- [Map method example](./AquaCare/Repositories/PlantRepository.cs#L190-L215)
- [Database Interaction Example](./AquaCare/Repositories/PlantRepository.cs#L136-L172)
2. [SRP] or Single Responsability principle: 
Each Repository responsible for one entity or their group, each metod in repositories responsible for one of CRUD operations
- [Reading from DB Example](./AquaCare/Repositories/PlantRepository.cs#L216-L250)
- [Adding to DB example](./AquaCare/Repositories/PlantRepository.cs#L312-L372)
