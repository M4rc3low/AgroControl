FROM maven:3.9-eclipse-temurin-21 AS build
WORKDIR /workspace
COPY src/telemetry/pom.xml ./pom.xml
RUN mvn -q -DskipTests dependency:go-offline
COPY src/telemetry/src ./src
RUN mvn -q -DskipTests package

FROM eclipse-temurin:21-jre
WORKDIR /app
COPY --from=build /workspace/target/agrocontrol-telemetry-*.jar app.jar
EXPOSE 8100
ENTRYPOINT ["java", "-jar", "/app/app.jar"]
